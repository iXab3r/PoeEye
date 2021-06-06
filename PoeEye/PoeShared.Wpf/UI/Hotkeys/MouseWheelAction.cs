using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MouseWheelAction
    {
        [EnumMember(Value = nameof(None))]
        None,
        [EnumMember(Value = nameof(WheelUp))]
        WheelUp,
        [EnumMember(Value = nameof(WheelDown))]
        WheelDown
    }
}