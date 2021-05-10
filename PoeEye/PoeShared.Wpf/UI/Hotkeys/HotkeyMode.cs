using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI.Hotkeys
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HotkeyMode
    {
        [EnumMember(Value = nameof(Click))]
        [Description("Click/Toggle")]
        Click,
        [EnumMember(Value = nameof(Hold))]
        [Description("Hold")]
        Hold,
    }
}