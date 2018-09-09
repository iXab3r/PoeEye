using System;
using JetBrains.Annotations;
using PoeWhisperMonitor.Chat;

namespace PoeWhisperMonitor
{
    public interface IPoeWhisperService
    {
        IObservable<PoeMessage> Messages { [NotNull] get; }
    }
}