using System.ComponentModel;
using System.Runtime.Serialization;

namespace PoeShared.UI
{
    public enum UserInputBlockType
    {
        [EnumMember(Value = nameof(None))]
        [Description("Allow user input")]
        None,
        [EnumMember(Value = nameof(All))]
        [Description("Block all input")]
        All,
        [EnumMember(Value = nameof(Keyboard))]
        [Description("Block keyboard input")]
        Keyboard,
        [EnumMember(Value = nameof(Mouse))]
        [Description("Block mouse input")]
        Mouse,
        [EnumMember(Value = nameof(MouseMove))]
        [Description("Block mouse movement")]
        MouseMove
    }
}