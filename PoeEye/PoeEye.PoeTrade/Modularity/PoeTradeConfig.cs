using System;
using PoeShared.Modularity;

namespace PoeEye.PoeTrade.Modularity
{
    internal sealed class PoeTradeConfig : IPoeEyeConfig
    {
        public int MaxSimultaneousRequestsCount { get; set; } = 1;

        public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromSeconds(30);

        public bool ProxyEnabled { get; set; } = false;
    }
}