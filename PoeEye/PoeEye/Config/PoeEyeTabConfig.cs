using System;
using PoeShared.Audio;
using PoeShared.PoeTrade;

namespace PoeEye.Config
{
    internal struct PoeEyeTabConfig
    {
        public TimeSpan RecheckTimeout { get; set; }

        public bool IsAutoRecheckEnabled { get; set; }

        public IPoeQueryInfo QueryInfo { get; set; }

        public AudioNotificationType NotificationType { get; set; }

        public string ApiModuleId { get; set; }

        public string CustomTabName { get; set; }

        public override string ToString()
        {
            return $"[Timeout: {RecheckTimeout}] Query: {QueryInfo}";
        }
    }
}