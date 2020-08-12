using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI.Hotkeys
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HotkeyMode
    {
        [Description("Click/Toggle - trigger state will be toggled on each click")]
        Click,
        [Description("Hold - button must be held to activate trigger")]
        Hold,
    }
}