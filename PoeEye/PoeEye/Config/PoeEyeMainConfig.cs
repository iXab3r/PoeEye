using PoeShared.Common;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    using System;
    using System.Collections.Generic;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Scaffolding;

    internal sealed class PoeEyeMainConfig : IPoeEyeConfig
    {
        private static readonly IDictionary<string, float> DefaultCurrenciesPriceInChaos = new Dictionary<string, float>
        {
            {KnownCurrencyNameList.BlessedOrb, 2},
            {KnownCurrencyNameList.CartographersChisel, 1},
            {KnownCurrencyNameList.ChaosOrb, 1},
            {KnownCurrencyNameList.ChromaticOrb, 0.5f},
            {KnownCurrencyNameList.DivineOrb, 7},
            {KnownCurrencyNameList.ExaltedOrb, 50},
            {KnownCurrencyNameList.GemcuttersPrism, 2},
            {KnownCurrencyNameList.JewellersOrb, 0.14f},
            {KnownCurrencyNameList.OrbOfAlchemy, 0.5f},
            {KnownCurrencyNameList.OrbOfAlteration, 0.05f},
            {KnownCurrencyNameList.OrbOfChance, 0.125f},
            {KnownCurrencyNameList.OrbOfFusing, 0.5f},
            {KnownCurrencyNameList.OrbOfRegret, 2},
            {KnownCurrencyNameList.OrbOfScouring, 1},
            {KnownCurrencyNameList.RegalOrb, 1},
            {KnownCurrencyNameList.VaalOrb, 2}
        };

        private IDictionary<string, float> currenciesPriceInChaos = DefaultCurrenciesPriceInChaos;

        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeMainConfig()
        {
        }

        public PoeEyeMainConfig([NotNull] PoeEyeMainConfig source)
        {
            Guard.ArgumentNotNull(() => source);

            source.TransferPropertiesTo(this);
        }

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

        public TimeSpan MaxRefreshTimeout { get; set; } = TimeSpan.FromMinutes(30);
    }
}