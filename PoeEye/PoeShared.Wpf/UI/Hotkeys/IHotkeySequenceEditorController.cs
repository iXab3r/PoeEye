using System;
using System.Windows.Input;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public interface IHotkeySequenceEditorController : IDisposableReactiveObject
    {
        HotkeyGesture ToggleRecordingHotkey { get; set; }
        bool IsRecording { get; }
        bool IsBusy { get; }
        bool EnableMouseClicksRecording { get; set; }
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
}