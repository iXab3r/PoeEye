using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PoeShared.Blazor.Services;

[JsonConverter(typeof(JsonStringEnumMemberConverter))] 
public enum JsSelectionRangeDirection
{
    [Description(nameof(Forward))]
    [EnumMember(Value = "forward")]
    Forward,
    [Description(nameof(Backward))]
    [EnumMember(Value = "backward")]
    Backward,
    [Description(nameof(None))]
    [EnumMember(Value = "none")]
    None,
}