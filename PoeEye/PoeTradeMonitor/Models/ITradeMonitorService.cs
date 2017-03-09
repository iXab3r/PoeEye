using System;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Models
{
    public interface ITradeMonitorService : IDisposableReactiveObject
    {
        IObservable<TradeModel> Trades { [NotNull] get; }
    }
}