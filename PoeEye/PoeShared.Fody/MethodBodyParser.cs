using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace PoeShared.Fody;

public class MethodBodyParser
{
    private readonly FluentLogIsEnabled weaver;

    public MethodBodyParser(FluentLogIsEnabled weaver)
    {
        this.weaver = weaver;
    }

    private bool TryToExtractBlock(
        MethodDefinition methodDefinition,
        Collection<Instruction> instructions,
        int startInstructionIdx,
        out InstructionBlock instructionBlock)
    {
        //For instance field
        // [0] = IL_0000: nop
        // [1] = IL_0001: ldarg.0
        // [2] = IL_0002: ldfld PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::logger
        // [3] = IL_0007: ldstr "message"
        // [4] = IL_000c: callvirt System.Void PoeShared.Logging.IFluentLog::Debug(System.String)
        // [5] = IL_0011: nop
        // [6] = IL_0012: ret

        //For static field
        // [0] = IL_0000: nop
        // [1] = IL_0001: ldsfld PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::StaticLogger
        // [2] = IL_0006: ldstr "message"
        // [3] = IL_000b: callvirt System.Void PoeShared.Logging.IFluentLog::Debug(System.String)
        // [4] = IL_0010: nop
        // [5] = IL_0011: ret

        //For method invocation
        // [0] = IL_0000: nop
        // [1] = IL_0001: ldarg.0
        // [2] = IL_0002: call PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::GetLogger()
        // [3] = IL_0007: ldstr "message"
        // [4] = IL_000c: callvirt System.Void PoeShared.Logging.IFluentLog::Debug(System.String)
        // [5] = IL_0011: nop
        // [6] = IL_0012: ret

        var startInstruction = instructions[startInstructionIdx];
        var startInstructionAsString = startInstruction.ToString();
        if (!weaver.LogInstructionsHash.Any(y => startInstructionAsString.Contains(y)))
        {
            instructionBlock = default;
            return false;
        }

        if (startInstruction.OpCode != OpCodes.Callvirt || startInstruction.Operand is not MethodReference methodReference)
        {
            weaver.WriteInfo($"Unsupported Logger invocation method {methodDefinition}, expected Callvirt, instructions:\n{string.Join(Environment.NewLine, instructions)}");
            instructionBlock = default;
            return false;
        }

        var i = startInstructionIdx;
        var startIndex = i;
        instructionBlock = new InstructionBlock();
        
        if (methodReference.HasParameters && methodReference.Parameters.Count == 1 && methodReference.Parameters[0].ParameterType.FullName == typeof(string).FullName)
        {
            if (instructions[i - 3].OpCode == OpCodes.Ldarg_0 &&
                instructions[i - 2].OpCode == OpCodes.Ldfld &&
                instructions[i - 2].Operand is FieldDefinition instanceField)
            {
                // logger in non-static field
                instructionBlock.LogInstructionIndex = 3;
                instructionBlock.FieldDefinition = instanceField;
                instructionBlock.Instructions.Add(instructions[i - 3]); // ldarg.0 = this
                instructionBlock.Instructions.Add(instructions[i - 2]); // ldfld PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::logger
                instructionBlock.Instructions.Add(instructions[i - 1]); // ldstr = "message"
                instructionBlock.Instructions.Add(instructions[i]); // callvirt = System.Void PoeShared.Logging.IFluentLog::Debug(System.String)

                instructionBlock.PrologueInstructionProducer = il => il.Create(OpCodes.Ldarg_0);
                instructionBlock.EpilogueInstructionProducer = (il, prologueInstruction) => { il.InsertAfter(prologueInstruction, il.Create(OpCodes.Ldfld, instanceField)); };

                if (weaver.IsDebugBuild)
                {
                    instructionBlock.Instructions.Add(instructions[i + 1]);
                }
            }
            else if (instructions[i - 2].OpCode == OpCodes.Ldsfld &&
                     instructions[i - 2].Operand is FieldDefinition staticField)
            {
                // logger in static field
                instructionBlock.LogInstructionIndex = 2;
                instructionBlock.FieldDefinition = staticField;

                instructionBlock.Instructions.Add(instructions[i - 2]); // ldsfld PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::StaticLogger
                instructionBlock.Instructions.Add(instructions[i - 1]); // ldstr "message"
                instructionBlock.Instructions.Add(instructions[i]); // callvirt System.Void PoeShared.Logging.IFluentLog::Debug(System.String)

                instructionBlock.PrologueInstructionProducer = il => il.Create(OpCodes.Ldsfld, staticField);
                instructionBlock.EpilogueInstructionProducer = (il, prologueInstruction) => { };
            }
            else
            {
                //unsupported logger invocation method
                weaver.WriteInfo($"Unsupported Logger args in {methodDefinition}, instructions:\n{string.Join(Environment.NewLine, instructions)}");
            }
        }

        if (instructionBlock.Instructions.Count == 0 || weaver.IsSurroundedWithIsEnabled(instructions, startIndex, instructionBlock))
        {
            return false;
        }

        return true;
    }
    
    private bool TryToExtractBlockForward(
        MethodDefinition methodDefinition,
        Collection<Instruction> instructions,
        int startInstructionIdx,
        out InstructionBlock instructionBlock)
    {
        var startInstruction = instructions[startInstructionIdx];

        if (startInstruction.Operand is not FieldDefinition loggerField || 
            loggerField.FieldType.FullName != FluentLogIsEnabled.LoggerField ||
            instructions.Count == startInstructionIdx)
        {
            instructionBlock = default;
            return false;
        }
        
        if (instructions[startInstructionIdx].OpCode != OpCodes.Ldsfld && instructions[startInstructionIdx].OpCode != OpCodes.Ldfld)
        {
            instructionBlock = default;
            return false;
        }
        
        if (instructions[startInstructionIdx + 1].OpCode == OpCodes.Stfld)
        {
            instructionBlock = default;
            return false;
        }

        if (instructions[startInstructionIdx + 1].OpCode == OpCodes.Callvirt && 
            instructions[startInstructionIdx + 1].Operand is MethodReference nextMethodReference &&
            nextMethodReference.DeclaringType == loggerField.FieldType)
        {
            //that is not in scope of logging method, this is probably property access as it does not have params
            instructionBlock = default;
            return false;
        }

        for (int i = startInstructionIdx; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            if (instruction.OpCode != OpCodes.Callvirt || 
                instruction.Operand is not MethodReference methodReference)
            {
                continue;
            }
            
            var instructionAsString = instruction.ToString();
            if (!weaver.LogInstructionsHash.Any(y => instructionAsString.Contains(y)))
            {
                continue;
            }

            instructionBlock = new InstructionBlock();
            instructionBlock.FieldDefinition = loggerField;

            var invocationInstructionCount = i - startInstructionIdx + 1;
            
            if (startInstruction.OpCode == OpCodes.Ldfld)
            {
                var invocationInstructions = instructions.Skip(startInstructionIdx - 1).Take(invocationInstructionCount + 1).ToArray();
                instructionBlock.Instructions.AddRange(invocationInstructions);
                instructionBlock.PrologueInstructionProducer = il => il.Create(OpCodes.Ldarg_0);
                instructionBlock.EpilogueInstructionProducer = (il, prologueInstruction) => { il.InsertAfter(prologueInstruction, il.Create(OpCodes.Ldfld, loggerField)); };
            } 
            else if (startInstruction.OpCode == OpCodes.Ldsfld)
            {
                var invocationInstructions = instructions.Skip(startInstructionIdx).Take(invocationInstructionCount).ToArray();
                instructionBlock.Instructions.AddRange(invocationInstructions);
                instructionBlock.PrologueInstructionProducer = il => il.Create(OpCodes.Ldsfld, loggerField);
                instructionBlock.EpilogueInstructionProducer = (il, prologueInstruction) => { };
            }
            else
            {
                throw new InvalidOperationException($"Instruction #{i} {instruction} was not expected at this point in method {methodDefinition}, start instruction #{startInstructionIdx} {startInstruction}\n{string.Join(Environment.NewLine, instructions)}");
            }
            instructionBlock.LogInstructionIndex = instructionBlock.Instructions.Count - 1;

            if (weaver.IsDebugBuild)
            {
                instructionBlock.Instructions.Add(instructions[i + 1]);
            }
            
            if (instructionBlock.Instructions.Count == 0 || weaver.IsSurroundedWithIsEnabled(instructions, i, instructionBlock))
            {
                return false;
            }

            weaver.WriteInfo($"Weaved method {methodDefinition}, successfully, start instruction #{startInstructionIdx} {startInstruction}, end instruction #{i}{instruction}\nInvocation:\n{string.Join(Environment.NewLine, instructionBlock.Instructions)}\n\nFull method code:\n{string.Join(Environment.NewLine, instructions)}");
            return true;
        }
        instructionBlock = new InstructionBlock();

        /*
        var startIndex = i;
        
        if (methodReference.HasParameters && methodReference.Parameters.Count == 1 && methodReference.Parameters[0].ParameterType.FullName == typeof(string).FullName)
        {
            if (instructions[i - 3].OpCode == OpCodes.Ldarg_0 &&
                instructions[i - 2].OpCode == OpCodes.Ldfld &&
                instructions[i - 2].Operand is FieldDefinition instanceField)
            {
                // logger in non-static field
                instructionBlock.LogInstructionIndex = 3;
                instructionBlock.FieldDefinition = instanceField;
                instructionBlock.Instructions.Add(instructions[i - 3]); // ldarg.0 = this
                instructionBlock.Instructions.Add(instructions[i - 2]); // ldfld PoeShared.Logging.IFluentLog LoggerIsEnabledScenarios::logger
                instructionBlock.Instructions.Add(instructions[i - 1]); // ldstr = "message"
                instructionBlock.Instructions.Add(instructions[i]); // callvirt = System.Void PoeShared.Logging.IFluentLog::Debug(System.String)

                instructionBlock.PrologueInstructionProducer = il => il.Create(OpCodes.Ldarg_0);
                instructionBlock.EpilogueInstructionProducer = (il, prologueInstruction) => { il.InsertAfter(prologueInstruction, il.Create(OpCodes.Ldfld, instanceField)); };

                if (weaver.IsDebugBuild)
                {
                    instructionBlock.Instructions.Add(instructions[i + 1]);
                    i += 1;
                }
            }
            else
            {
                //unsupported logger invocation method
                weaver.WriteInfo($"Unsupported Logger args in {methodDefinition}, instructions:\n{string.Join(Environment.NewLine, instructions)}");
            }
        }*/

        /*if (instructionBlock.Instructions.Count == 0 || weaver.IsSurroundedWithIsEnabled(instructions, startIndex, instructionBlock))
        {
            return false;
        }*/

        return true;
    }

    public List<InstructionBlock> GetLoggingInstructionBlocksWithoutIsEnabled(MethodDefinition methodDefinition, Collection<Instruction> instructions)
    {
        var instructionBlocks = new List<InstructionBlock>();

        for (var i = 0; i < instructions.Count; i++)
        {
            if (TryToExtractBlockForward(methodDefinition, instructions, i, out var instructionBlock) && instructionBlock != null && instructionBlock.Instructions.Count > 0)
            {
                instructionBlocks.Add(instructionBlock);
            }
        }

        return instructionBlocks;
    }
}