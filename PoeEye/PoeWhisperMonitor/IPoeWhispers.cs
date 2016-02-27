namespace PoeWhisperMonitor
{
    using System;

    using JetBrains.Annotations;

    using PoeShared.Chat;

    public interface IPoeWhispers
    {
        IObservable<PoeMessage> Messages { [NotNull] get; }
    }
}