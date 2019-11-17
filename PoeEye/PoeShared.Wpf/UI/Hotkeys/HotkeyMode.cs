using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI.Hotkeys
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum HotkeyMode
    {
        Click,
        Hold,
    }
}