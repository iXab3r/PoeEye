namespace PoeWhisperMonitor
{
    using System;

    using Chat;

    using JetBrains.Annotations;

    public interface IPoeWhisperService
    {
        IObservable<PoeMessage> Messages { [NotNull] get; }
    }
}