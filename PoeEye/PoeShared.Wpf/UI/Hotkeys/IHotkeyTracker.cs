using System;
using System.Collections.ObjectModel;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public interface IHotkeyTracker : IDisposableReactiveObject
    {
        bool IsActive { get; set; }
        
        [Obsolete("Replaced with Hotkeys that supports multiple keys")]
        HotkeyGesture Hotkey { get; set; }
        
        ReadOnlyObservableCollection<HotkeyGesture> Hotkeys { get; }

        HotkeyMode HotkeyMode { get; set; }
        
        bool SuppressKey { get; set; }

        void Add(HotkeyGesture hotkeyToAdd);

        void Remove(HotkeyGesture hotkeyToRemove);

        void Clear();
    }
}