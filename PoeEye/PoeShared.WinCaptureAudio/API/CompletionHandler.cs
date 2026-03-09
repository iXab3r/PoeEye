using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wasapi.CoreAudioApi.Interfaces;
using PoeShared.WinCaptureAudio.Extensions;
using HResult = PInvoke.HResult;

namespace PoeShared.WinCaptureAudio.API;

[StructLayout(LayoutKind.Sequential)]
internal class CompletionHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
{
    public readonly nint eventFinished;
    public IAudioClient AudioClient { get; private set; }

    public HResult.Code Result { get; private set; } = HResult.Code.E_FAIL;

    public CompletionHandler()
    {
        eventFinished = Kernel32Api.CreateEvent(nint.Zero, false, false, null);
    }

    public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation operation)
    {
        operation.GetActivateResult(out var hrCode, out var obj);
        Result = (HResult.Code) hrCode;
        AudioClient = (IAudioClient) obj;
        Kernel32Api.SetEvent(eventFinished);
    }
}