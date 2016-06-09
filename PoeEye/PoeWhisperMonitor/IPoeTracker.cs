using System;
using JetBrains.Annotations;

namespace PoeWhisperMonitor
{
    internal interface IPoeTracker : IDisposable
    {
        IObservable<PoeProcessInfo[]> ActiveProcesses { [NotNull] get; }
    }
}