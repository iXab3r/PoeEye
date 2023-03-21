using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services;

internal sealed class ProcessAudioController : DisposableReactiveObjectWithLogger, IProcessAudioController
{
    public void SetIsMutedByProcessId(bool isMuted, int processId)
    {
        SetIsMuted(isMuted, processId);
    }

    public void SetIsMutedByProcessName(bool isMuted, string processName)
    {
        var processId = Process.GetProcessesByName(processName);
        processId.ForEach(process => SetIsMutedByProcessId(isMuted, process.Id));
    }

    private static void SetIsMuted(Action<AudioSessionControl, SimpleAudioVolume> controller, int targetProcessId)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var captureDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        for (var idx = 0; idx < captureDevice.AudioSessionManager.Sessions.Count; idx++)
        {
            var session = captureDevice.AudioSessionManager.Sessions[idx];

            uint processId = default;
            try
            {
                processId = session.GetProcessID;
            }
            catch (Exception e)
            {
                // do nothing, some processes will not be available
            }

            if (processId == targetProcessId)
            {
                controller(session, session.SimpleAudioVolume);
            }
        }
    }

    private void SetIsMuted(bool isMuted, int processId)
    {
        SetIsMuted((sessionControl, volumeController) =>
        {
            Log.Info(() => $"Matched process {sessionControl.GetProcessID}, setting {nameof(SimpleAudioVolume.Mute)} to {isMuted} using {volumeController}");
            volumeController.Mute = isMuted;
        }, processId);
    }
}