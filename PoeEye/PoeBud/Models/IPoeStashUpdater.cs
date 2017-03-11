using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeBud.Models
{
    public interface IPoeStashUpdater : IDisposableReactiveObject
    {
        TimeSpan RecheckPeriod { get; set; }

        DateTime LastUpdateTimestamp { get; }

        bool IsBusy { get; }

        IObservable<StashUpdate> Updates { [NotNull] get; }

        IObservable<Exception> UpdateExceptions { [NotNull] get; }

        void ForceRefresh();
    }
}