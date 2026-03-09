using System.Diagnostics;
using NAudio.Wave;
using PoeShared.Audio.Models;
using PoeShared.Scaffolding;

namespace PoeShared.WinCaptureAudio;

public class ProcessWaveInListener : WaveInListener
{
    private readonly ProcessAudioCapture capture;

    public ProcessWaveInListener(Process process, WaveFormat outputFormat) : base(outputFormat)
    {
        capture = new ProcessAudioCapture(process).AddTo(Anchors);
    }

    protected override IWaveIn GetSource()
    {
        return capture;
    }
}