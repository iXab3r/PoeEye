using System;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using PoeShared.Fody;

public sealed class FluentLogIsEnabled : BaseModuleWeaver
{
    public static readonly string LoggerField = "PoeShared.Logging.IFluentLog";
    public static readonly string LogIsEnabledMethodField = "PoeShared.Logging.FluentLogExtensions::IsEnabled";
    private List<EnumValue> logLevelValues;
    private MethodBodyParser methodBodyParser;

    public FluentLogIsEnabled()
    {
        methodBodyParser = new MethodBodyParser(this);
    }

    public override void Execute()
    {
        WriteInfo("Weaving ILogger with IsEnabled - Start");
        Init();
        ProcessAssembly();
        WriteInfo("Weaving ILogger with IsEnabled - End");
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "netstandard";
        yield return "PoeShared";
    }

    public override bool ShouldCleanReference => false;
    
    public bool IsDebugBuild { get; private set; }

    public HashSet<string> LogInstructionsHash { get; private set; }
    
    public IDictionary<int, string> LogIsEnabledMethodNameByValue { get; private set; }

    private void Init()
    {
        IsDebugBuild = ModuleDefinition.Assembly.IsDebugBuild();
        logLevelValues = GetEnumValuesOfType("PoeShared.Logging.FluentLogLevel");
        LogInstructionsHash = new HashSet<string>(logLevelValues.Select(x => $"{LoggerField}::" + x.Name));
        LogIsEnabledMethodNameByValue = logLevelValues
            .ToDictionary(x => x.Value, x => $"get_Is{x.Name}Enabled");
    }

    private void ProcessAssembly()
    {
        foreach (var type in ModuleDefinition.GetTypes())
        {
            if (!IsApplicableTypeDefinition(type))
            {
                WriteDebug($"Skipping type {type.Name}, not applicable");
                continue;
            }

            var iLoggerField = type.GetFieldDefintion(LoggerField);
            if (iLoggerField == null)
            {
                WriteDebug($"Skipping type {type.Name}, ILogger field not found");
                continue;
            }

            WriteDebug($"Processing type {type.Name}");

            foreach (var method in type.Methods)
            {
                if (!IsApplicableMethodDefinition(method))
                {
                    WriteDebug($"Skipping method {method.Name}, not applicable");
                    continue;
                }

                WriteDebug($"Processing method {method.Name}");

                ProcessMethod(method);
            }
        }
    }

    private bool IsApplicableTypeDefinition(TypeDefinition typeDefinition)
    {
        return !typeDefinition.IsEnum
            && !typeDefinition.IsInterface;
    }

    private bool IsApplicableMethodDefinition(MethodDefinition methodDefinition)
    {
        return methodDefinition.HasBody;
    }

    private void ProcessMethod(MethodDefinition methodDefinition)
    {
        var methodBody = methodDefinition.Body;
        var loggingInstructionBlocks = methodBodyParser.GetLoggingInstructionBlocksWithoutIsEnabled(methodDefinition, methodBody.Instructions);

        methodBody.SimplifyMacros();
        for (int i = loggingInstructionBlocks.Count - 1; i >= 0; i--)
        {
            WriteDebug($"Weaving logging block: [{i}]");
            RewriteLoggingInBody(methodBody, loggingInstructionBlocks[i]);
        }
        methodBody.OptimizeMacros();
    }
    

    public bool IsSurroundedWithIsEnabled(Collection<Instruction> instructions, int startIndex, InstructionBlock instructionBlock)
    {
        // IL_0001: ldarg.0      // this
        // IL_0002: ldfld        class [Microsoft.Extensions.Logging.Abstractions]Microsoft.Extensions.Logging.ILogger LoggerIsEnabledScenarios::_logger
        // IL_0007: ldc.i4.0
        // IL_0008: callvirt     instance bool [Microsoft.Extensions.Logging.Abstractions]Microsoft.Extensions.Logging.ILogger::IsEnabled(valuetype [Microsoft.Extensions.Logging.Abstractions]Microsoft.Extensions.Logging.LogLevel)
        // IL_000d: stloc.0      // V_0
        // IL_000e: ldloc.0      // V_0
        // IL_000f: brfalse.s    IL_0030

        for (int index = startIndex; index >= 0; index--)
        {
            var brfalseSInstruction = instructions[index];
            if (brfalseSInstruction.OpCode != OpCodes.Brfalse_S)
            {
                continue;
            }

            var isEnabledCallInstructionIndex = index - (IsDebugBuild ? 3 : 1);
            if (isEnabledCallInstructionIndex <= 0)
            {
                continue;
            }

            var isEnabledCallInstruction = instructions[isEnabledCallInstructionIndex];
            var isEnabledCallInstructionAsString = isEnabledCallInstruction.ToString();
            if (!isEnabledCallInstructionAsString.Contains(LogIsEnabledMethodField) && !LogIsEnabledMethodNameByValue.Values.Any(y => isEnabledCallInstructionAsString.Contains(y)))
            {
                continue;
            }

            if (!(brfalseSInstruction.Operand is Instruction instructionOperand))
            {
                continue;
            }

            if (IsInstructionAfterInstructionBlock(instructionBlock, instructionOperand))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInstructionAfterInstructionBlock(InstructionBlock instructionBlock, Instruction instruction)
    {
        return instruction.Offset > instructionBlock.Instructions.Last().Offset;
    }

    private void RewriteLoggingInBody(MethodBody methodBody, InstructionBlock loggingInstructionBlock)
    {
        var logInstruction = loggingInstructionBlock.Instructions[loggingInstructionBlock.LogInstructionIndex];
        var logLevel = logLevelValues.First(x => logInstruction.ToString().Contains(x.Name));

        WriteDebug($"Weaving logging block for LogLevel: [{logLevel.Name}]");

        if (IsDebugBuild)
        {
            methodBody.Variables.Add(new VariableDefinition(ModuleDefinition.TypeSystem.Boolean));
        }

        var il = methodBody.GetILProcessor();

        var firstInstruction = loggingInstructionBlock.PrologueInstructionProducer(il);
        var beginInstruction = loggingInstructionBlock.Instructions[0];

        il.InsertBefore(beginInstruction, firstInstruction);

        ReplaceExceptionHandlerStartWithFirstInstruction(methodBody, beginInstruction, firstInstruction);
        ReplacePreviousBeginInstructionOffsetsWithFirstInstruction(il, methodBody, beginInstruction, firstInstruction);

        loggingInstructionBlock.EpilogueInstructionProducer(il, firstInstruction);
        var logIsEnabledMethodName = LogIsEnabledMethodNameByValue[logLevel.Value];
        il.InsertBefore(beginInstruction, il.Create(OpCodes.Callvirt, CreateMethodReference(LoggerField, logIsEnabledMethodName)));

        var lastInstruction = loggingInstructionBlock.Instructions.Last();
        if (IsDebugBuild)
        {
            il.InsertBefore(beginInstruction, il.Create(OpCodes.Stloc_0));
            il.InsertBefore(beginInstruction, il.Create(OpCodes.Ldloc_0));
            var nop = il.Create(OpCodes.Nop);
            il.InsertAfter(lastInstruction, nop);
            lastInstruction = nop;
        }

        var gotoInstruction = FindGotoInstruction(methodBody, lastInstruction);

        il.InsertBefore(beginInstruction, il.Create(OpCodes.Brfalse_S, gotoInstruction));

        if (IsDebugBuild)
        {
            il.InsertBefore(beginInstruction, il.Create(OpCodes.Nop));
            methodBody.InitLocals = true;
        }
    }

    private static Instruction FindGotoInstruction(MethodBody methodBody, Instruction lastInstruction)
    {
        var gotoInstruction = lastInstruction.Next;
        if (gotoInstruction.OpCode != OpCodes.Ret)
        {
            return gotoInstruction;
        }

        var startIndex = methodBody.Instructions.IndexOf(gotoInstruction);
        for (int index = startIndex + 1; index < methodBody.Instructions.Count; index++)
        {
            var instruction = methodBody.Instructions[index];
            if (instruction.OpCode == OpCodes.Ret)
            {
                gotoInstruction = instruction;
                break;
            }
        }

        return gotoInstruction;
    }

    private void ReplacePreviousBeginInstructionOffsetsWithFirstInstruction(ILProcessor il, MethodBody methodBody, Instruction beginInstruction, Instruction firstInstruction)
    {
        var beginInstructionIndex = methodBody.Instructions.IndexOf(beginInstruction);

        for (int index = beginInstructionIndex; index >= 0; index--)
        {
            var instruction = methodBody.Instructions[index];
            if (!(instruction.Operand is Instruction instructionOperand))
            {
                continue;
            }

            if (instructionOperand.Offset == beginInstruction.Offset)
            {
                il.Replace(instruction, il.Create(instruction.OpCode, firstInstruction));
            }
        }
    }

    private void ReplaceExceptionHandlerStartWithFirstInstruction(MethodBody methodBody, Instruction beginInstruction, Instruction firstInstruction)
    {
        if (!methodBody.HasExceptionHandlers)
        {
            return;
        }

        foreach (var exceptionHandler in methodBody.ExceptionHandlers)
        {
            if (beginInstruction.Offset != exceptionHandler.TryStart.Offset)
            {
                continue;
            }

            if (exceptionHandler.TryStart is Instruction)
            {
                exceptionHandler.TryStart = firstInstruction;
                return;
            }
        }
    }

    private MethodReference CreateMethodReference(string typeName, string methodName)
    {
        var methodRef = FindTypeDefinition(typeName).Methods.Single(x => x.Name == methodName);
        return ModuleDefinition.ImportReference(methodRef);
    }

    private List<EnumValue> GetEnumValuesOfType(string typeName)
    {
        var type = FindTypeDefinition(typeName);
        if (!type.IsEnum)
        {
            return Enumerable.Empty<EnumValue>().ToList();
        }

        var enumValues = new List<EnumValue>();
        foreach (var field in type.Fields)
        {
            if (!field.HasConstant)
            {
                continue;
            }

            enumValues.Add(new EnumValue { Value = (int)field.Constant, Name = field.Name });
        }

        return enumValues;
    }
}