using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI;

public interface IHotkeySequenceEditorViewModel : IDisposableReactiveObject
{
    bool HideKeypressDelays { get; set; }
    bool MaxItemsExceeded { get; }
    bool MaxDurationExceeded { get; }
    int TotalCount { get; }
    int MaxItemsCount { get; set; }
    TimeSpan TotalDuration { get; }
    TimeSpan MaxDuration { get; set; }
    TimeSpan DefaultKeyPressDuration { get; set; }
    ObservableCollection<HotkeySequenceItem> Items { get; }
    ICommand AddItem { get; }
    ICommand RemoveItem { get; }
    ICommand ClearItems { get; }
    IHotkeySequenceEditorController Controller { get; }
}