using System;
using System.Reactive;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade
{
    public interface IPoePriceCalculcator
    {
        [NotNull]
        IObservable<Unit> WhenChanged { get; }

        bool CanConvert(PoePrice price);

        PoePrice GetEquivalentInChaosOrbs(PoePrice price);
    }
}