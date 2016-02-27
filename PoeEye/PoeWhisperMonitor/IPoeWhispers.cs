namespace PoeWhisperMonitor
{
    using System;

    using PoeShared.Chat;

    public interface IPoeWhispers
    {
        IObservable<PoeMessage> Messages { get; }
    }
}