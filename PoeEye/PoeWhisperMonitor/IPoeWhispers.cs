using System;
using PoeShared.Chat;

namespace PoeWhisperMonitor
{
    public interface IPoeWhispers
    {
        IObservable<PoeMessage> Messages { get; }
    }
}