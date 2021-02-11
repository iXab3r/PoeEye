using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.UI.Hotkeys
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MouseWheelAction
    {
        None,
        WheelUp,
        WheelDown
    }
}