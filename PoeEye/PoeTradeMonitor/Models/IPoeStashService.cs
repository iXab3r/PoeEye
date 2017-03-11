using System;
using System.Reactive;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.TradeMonitor.Models
{
    internal interface IPoeStashService : IDisposableReactiveObject
    {
        IObservable<Unit> Updates { [NotNull] get; }

        [CanBeNull] 
        IStashItem TryToFindItem([CanBeNull] string tabName, int itemX, int itemY);

        DateTime LastUpdateTimestamp { get; }
    }
}