using System;
using System.Reactive;
using JetBrains.Annotations;

namespace PoeOracle.Models
{
    public interface IExternalUriOpener
    {
        IObservable<Unit> Requested { [NotNull] get; }
        void Request([NotNull] string uri);
    }
}