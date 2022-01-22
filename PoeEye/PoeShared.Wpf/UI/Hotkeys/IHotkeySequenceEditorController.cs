using System;
using System.Drawing;
using System.Windows.Input;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IHotkeySequenceEditorController : IDisposableReactiveObject
{
    Fallback<HotkeyGesture> ToggleRecordingHotkey { get; set; }
    bool IsRecording { get; }
    bool IsBusy { get; }
    bool EnableMouseClicksRecording { get; set; }
    Point? MouseLocation { get; }
    DateTimeOffset? RecordStartTime { get; }
    MousePositionRecordingType MousePositionRecording { get; set; }
    bool EnableKeyboardRecording { get; set; }  
    IWindowHandle TargetWindow { get; set; }
    TimeSpan MousePositionRecordingResolution { get; set; }
    ICommand StopRecording { get; }
    ICommand StartRecording { get; }
    TimeSpan TotalDuration { get; }
    IHotkeySequenceEditorViewModel Owner { get; }
}