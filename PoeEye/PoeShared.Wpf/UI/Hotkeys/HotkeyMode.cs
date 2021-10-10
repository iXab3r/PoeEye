using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HotkeyMode
    {
        [EnumMember(Value = nameof(Click))]
        [Description("Toggle")]
        Click,
        [EnumMember(Value = nameof(Hold))]
        [Description("Click/Hold")]
        Hold,
    }
}