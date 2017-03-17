using System;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Services
{
    internal interface ITradeMonitorService : IDisposableReactiveObject
    {
        IObservable<TradeModel> Trades { [NotNull] get; }
    }
}