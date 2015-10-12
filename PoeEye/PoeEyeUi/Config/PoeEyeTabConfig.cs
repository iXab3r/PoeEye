namespace PoeEyeUi.Config
{
    using System;

    using PoeShared.PoeTrade;

    using PoeTrade.Models;

    internal struct PoeEyeTabConfig
    {
        public TimeSpan RecheckTimeout { get; set; }

        public IPoeQueryInfo QueryInfo { get; set; }

        public override string ToString()
        {
            return $"[Timeout: {RecheckTimeout}] Query: {QueryInfo}";
        }
    }
}