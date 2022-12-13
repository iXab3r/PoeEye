using System;
using System.Diagnostics;

namespace PoeShared.Audio.Services;

public interface IProcessAudioController
{
    void SetIsMutedByProcessId(bool isMuted, int processId);
    void SetIsMutedByProcessName(bool isMuted, string processName);
    void SetIsMuted(bool isMuted, Predicate<Process> processMatcher);
    void ToggleIsMuted(Predicate<Process> processMatcher);
}