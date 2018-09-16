using System;
using System.Collections.Generic;
using PoeShared.Common;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeMainConfig : IPoeEyeConfigVersioned
    {
        private static readonly IDictionary<string, float> DefaultCurrenciesPriceInChaos = new Dictionary<string, float>
        {
            {KnownCurrencyNameList.ChaosOrb, 1},
            {KnownCurrencyNameList.BlessedOrb, 2},
            {KnownCurrencyNameList.CartographersChisel, 1},
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
            {KnownCurrencyNameList.VaalOrb, 2},
            {KnownCurrencyNameList.OrbOfTransmutation, 0.05f},
            {KnownCurrencyNameList.ApprenticeSextant, 2f},
            {KnownCurrencyNameList.JourneymanSextant, 5f},
            {KnownCurrencyNameList.MasterSextant, 8f},
            {KnownCurrencyNameList.GlassblowersBauble, 0.5f},
            {KnownCurrencyNameList.SplinterOfEsh, 0.1f},
            {KnownCurrencyNameList.SplinterOfTul, 0.1f},
            {KnownCurrencyNameList.SplinterOfXoph, 0.03f},
            {KnownCurrencyNameList.SplinterOfChayula, 2f},
            {KnownCurrencyNameList.SplinterOfUulNetol, 0.15f}
        };

        private IDictionary<string, float> currenciesPriceInChaos = new Dictionary<string, float>(DefaultCurrenciesPriceInChaos);

        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeTabConfig[] TabConfigs
        {
            get => tabConfigs;
            set => tabConfigs = value ?? new PoeEyeTabConfig[0];
        }

        public IDictionary<string, float> CurrenciesPriceInChaos
        {
            get => currenciesPriceInChaos;
            set => currenciesPriceInChaos = value ?? new Dictionary<string, float>(DefaultCurrenciesPriceInChaos);
        }

        public bool ClipboardMonitoringEnabled { get; set; } = true;

        public bool WhisperNotificationsEnabled { get; set; } = false;

        public TimeSpan MinRefreshTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public TimeSpan MaxRefreshTimeout { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan ProxyRecheckTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public string LeagueId { get; set; }

        public int ItemPageSize { get; set; } = 20;

        public int Version { get; set; } = 2;
    }
}