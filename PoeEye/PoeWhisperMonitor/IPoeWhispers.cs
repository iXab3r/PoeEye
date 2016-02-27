namespace PoeWhisperMonitor
{
    using System;

    using Chat;

    using JetBrains.Annotations;

    public interface IPoeWhispers
    {
        IObservable<PoeMessage> Messages { [NotNull] get; }
    }
}