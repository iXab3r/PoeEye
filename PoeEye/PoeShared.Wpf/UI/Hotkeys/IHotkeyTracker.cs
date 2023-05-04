using System.Collections.ObjectModel;
using PoeShared.Hotkeys;
using PoeShared.Scaffolding; 

namespace PoeShared.UI;

public interface IHotkeyTracker : IDisposableReactiveObject
{
    bool IsActive { get; }
        
    /// <summary>
    ///   Could be used in conjunction with Hotkeys collection to support multiple hotkeys
    /// </summary>
    HotkeyGesture Hotkey { get; set; }
        
    ReadOnlyObservableCollection<HotkeyGesture> Hotkeys { get; }

    HotkeyMode HotkeyMode { get; set; }
        
    bool SuppressKey { get; set; }
        
    bool CanSuppressHotkey { get; }

    bool IgnoreModifiers { get; set; }
        
    bool IsEnabled { get; set; }
        
    bool HandleApplicationKeys { get; set; }
        
    bool HasModifiers { get; }

    void Add(params HotkeyGesture[] hotkeysToAdd);

    void Remove(params HotkeyGesture[] hotkeysToRemove);

    void Clear();

    void Reset();
    
    void Activate();
}