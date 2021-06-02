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
        bool IsRecording { get; }
        int TotalItemsCount { get; }
        int MaxItemsCount { get; set; }
        TimeSpan TotalDuration { get; }
        TimeSpan MaxDuration { get; set; }
        TimeSpan DefaultKeyPressDuration { get; set; }
        TimeSpan MousePositionRecordingResolution { get; set; }
        ObservableCollection<HotkeySequenceItem> Items { get; }
        HotkeyGesture StopRecordingHotkey { get; set; }
        ICommand AddItem { get; }
        ICommand RemoveItem { get; }
        ICommand ClearItems { get; }
        ICommand StopRecording { get; }
    }
}