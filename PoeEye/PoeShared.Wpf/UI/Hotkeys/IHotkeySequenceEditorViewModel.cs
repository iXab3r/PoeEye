using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Hotkeys
{
    public interface IHotkeySequenceEditorViewModel : IDisposableReactiveObject
    {
        bool EnableMouseClicksRecording { get; set; }
        bool EnableMousePositionRecording { get; set; }
        bool EnableKeyboardRecording { get; set; }
        bool HideKeypressDelays { get; set; }
        int MaxItemsCount { get; set; }
        TimeSpan MaxDuration { get; }
        TimeSpan DefaultKeyPressDuration { get; set; }
        TimeSpan MousePositionRecordingResolution { get; set; }
        ObservableCollection<HotkeySequenceItem> Items { get; }
        ICommand AddItem { get; }
        ICommand RemoveItem { get; }
        ICommand ClearItems { get; }
    }
}