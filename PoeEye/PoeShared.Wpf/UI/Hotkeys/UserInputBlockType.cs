using System.ComponentModel;
using System.Runtime.Serialization;

namespace PoeShared.UI;

public enum UserInputBlockType
{
    [EnumMember(Value = nameof(None))]
    [Description("Disabled")]
    None,
    [EnumMember(Value = nameof(All))]
    [Description("Block All input")]
    All,
    [EnumMember(Value = nameof(Keyboard))]
    [Description("Block Keyboard input")]
    Keyboard,
    [EnumMember(Value = nameof(Mouse))]
    [Description("Block Mouse input")]
    Mouse,
    [EnumMember(Value = nameof(MouseMove))]
    [Description("Block Mouse movement")]
    MouseMove
}