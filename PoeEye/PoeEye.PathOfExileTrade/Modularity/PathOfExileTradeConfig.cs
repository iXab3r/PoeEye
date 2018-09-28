using System;
using PoeShared.Modularity;

namespace PoeEye.PathOfExileTrade.Modularity
{
    internal sealed class PathOfExileTradeConfig : IPoeEyeConfigVersioned
    {
        public int MaxSimultaneousRequestsCount { get; set; } = 4;

        public int MaxItemsToFetch { get; set; } = 50;

        public TimeSpan DelayBetweenRequests { get; set; } = TimeSpan.FromSeconds(0);

        public bool ProxyEnabled { get; set; } = false;

        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public int Version { get; set; } = 4;
    }
}