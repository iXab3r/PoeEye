using System;
using System.Collections.Generic;
using System.Diagnostics;
using CSCore.CoreAudioAPI;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services;

internal sealed class ProcessAudioController : DisposableReactiveObjectWithLogger, IProcessAudioController
{
    public void SetIsMutedByProcessId(bool isMuted, int processId)
    {
        SetIsMuted(isMuted, x => x.Id == processId);
    }
    
    public void SetIsMutedByProcessName(bool isMuted, string processName)
    {
        SetIsMuted(isMuted, x => x.ProcessName == processName);
    }

    public void ToggleIsMuted(Predicate<Process> processMatcher)
    {
        SetIsMuted((sessionControl, volumeController) =>
        {
            var newState = !volumeController.IsMuted;
            Log.Info(() => $"Matched process {sessionControl.Process}, toggling {nameof(SimpleAudioVolume.IsMuted)} using {volumeController}, isMuted: {volumeController.IsMuted} => {newState}");
            volumeController.IsMuted = newState;
        }, processMatcher);
    }

    private void SetIsMuted(Action<AudioSessionControl2, SimpleAudioVolume> controller, Predicate<Process> processMatcher)
    {
        foreach (var sessionManager in GetDefaultAudioSessionManager2(DataFlow.Render))
        {
            using (sessionManager)
            using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
            {
                foreach (var session in sessionEnumerator)
                {
                    using var sessionControl = session.QueryInterface<AudioSessionControl2>();
                    if (sessionControl.IsDisposed)
                    {
                        continue;
                    }

                    try
                    {
                        if (!processMatcher(sessionControl.Process))
                        {
                            continue;
                        }
                        
                        using var simpleVolume = session.QueryInterface<SimpleAudioVolume>();
                        if (simpleVolume.IsDisposed)
                        {
                            continue;
                        }
                        
                        controller(sessionControl, simpleVolume);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to execute predicate for process {sessionControl.Process}", e);
                    }
                }
            }
        }
    }
    
    public void SetIsMuted(bool isMuted, Predicate<Process> processMatcher)
    {
        SetIsMuted((sessionControl, volumeController) =>
        {
            Log.Info(() => $"Matched process {sessionControl.Process}, setting {nameof(SimpleAudioVolume.IsMuted)} to {isMuted} using {volumeController}");
            volumeController.IsMuted = isMuted;
        }, processMatcher);
    }

    private static IEnumerable<AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var devices = enumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active);
        foreach (var device in devices)
        {
            var sessionManager = AudioSessionManager2.FromMMDevice(device);
            yield return sessionManager;
        }
    }
}