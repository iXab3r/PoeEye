namespace PoeEyeUi.Config
{
    using System;

    using PoeTrade.Models;

    internal struct PoeEyeTabConfig
    {
        public TimeSpan RecheckTimeout { get; set; }

        public IPoeQueryInfo QueryInfo { get; set; }
    }
}