﻿namespace PoeEye.Config
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Annotations;

    internal interface IPoeEyeConfig
    {
        PoeEyeTabConfig[] TabConfigs { [NotNull] get; [NotNull] set; }

        IDictionary<string, float> CurrenciesPriceInChaos { [NotNull] get; [NotNull] set; }

        bool ClipboardMonitoringEnabled { get; set; }

        bool AudioNotificationsEnabled { get; set; }

        bool WhisperNotificationsEnabled { get; set; }

        TimeSpan MinRefreshTimeout { get; set; }

        TimeSpan MaxRefreshTimeout { get; set; }
    }
}