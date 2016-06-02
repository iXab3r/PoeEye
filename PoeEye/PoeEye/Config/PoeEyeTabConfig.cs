namespace PoeEye.Config
{
    using System;

    using PoeShared.Common;
    using PoeShared.PoeTrade;

    internal struct PoeEyeTabConfig
    {
        public TimeSpan RecheckTimeout { get; set; }

        public bool IsAutoRecheckEnabled { get; set; }

        public IPoeQueryInfo QueryInfo { get; set; }

        public IPoeItem[] SoldOrRemovedItems { get; set; }

        public bool AudioNotificationEnabled { get; set; }

        public string ApiModuleName { get; set; }

        public override string ToString()
        {
            return $"[Timeout: {RecheckTimeout}] Query: {QueryInfo}";
        }
    }
}