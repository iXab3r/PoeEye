using System;
using PoeShared.Modularity;

namespace PoeEye.PoeTrade.Modularity
{
    internal sealed class PoeTradeConfig : IPoeEyeConfigVersioned
    {
        public int MaxSimultaneousRequestsCount { get; set; } = 4;

        public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromSeconds(0);

        public bool ProxyEnabled { get; set; } = false;

        public int Version { get; set; } = 2;
    }
}