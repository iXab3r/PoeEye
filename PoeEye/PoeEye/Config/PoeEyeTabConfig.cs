using Exceptionless.Json;
using PoeEye.PoeTrade;
using PoeEye.PoeTrade.Common;
using PoeShared.Audio;

namespace PoeEye.Config
{
    using System;

    using PoeShared.PoeTrade;

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