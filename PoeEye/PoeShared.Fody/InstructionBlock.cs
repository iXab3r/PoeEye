using System;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using Mono.Cecil;

public class InstructionBlock
{
    public List<Instruction> Instructions { get; } = new();
    public int LogInstructionIndex { get; set; }
    public FieldDefinition FieldDefinition { get; set; }
    
    public Func<ILProcessor, Instruction> PrologueInstructionProducer { get; set; }
    public Action<ILProcessor, Instruction> EpilogueInstructionProducer { get; set; }
}
