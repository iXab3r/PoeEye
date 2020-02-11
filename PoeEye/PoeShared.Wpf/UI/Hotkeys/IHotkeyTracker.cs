using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public interface IHotkeyTracker : IDisposableReactiveObject
    {
        bool IsActive { get; set; }
        
        HotkeyGesture Hotkey { get; set; }
        
        HotkeyMode HotkeyMode { get; set; }
        
        bool SuppressKey { get; set; }
    }
}