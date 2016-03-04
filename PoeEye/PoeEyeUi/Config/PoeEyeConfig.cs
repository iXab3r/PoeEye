namespace PoeEyeUi.Config
{
    using System;
    using System.Collections.Generic;

    internal sealed class PoeEyeConfig : IPoeEyeConfig
    {
        private static readonly IDictionary<string, float> DefaultCurrenciesPriceInChaos = new Dictionary<string, float>
        {
            {"blessed", 2},
            {"chisel", 1},
            {"chaos", 1},
            {"chromatic", 0.5f},
            {"divine", 7},
            {"exalted", 50},
            {"gcp", 2},
            {"jewellers", 0.14f},
            {"alchemy", 0.5f},
            {"alteration", 0.05f},
            {"chance", 0},
            {"fusing", 0.5f},
            {"regret", 2},
            {"scouring", 1},
            {"regal", 1}
        };

        private IDictionary<string, float> currenciesPriceInChaos = DefaultCurrenciesPriceInChaos;

        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeTabConfig[] TabConfigs
        {
            get { return tabConfigs; }
            set { tabConfigs = value ?? new PoeEyeTabConfig[0]; }
        }

        public IDictionary<string, float> CurrenciesPriceInChaos
        {
            get { return currenciesPriceInChaos; }
            set { currenciesPriceInChaos = value ?? DefaultCurrenciesPriceInChaos; }
        }

        public bool ClipboardMonitoringEnabled { get; set; } = true;

        public bool AudioNotificationsEnabled { get; set; } = true;

        public bool WhisperNotificationsEnabled { get; set; } = false;

        public TimeSpan MinRefreshTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}