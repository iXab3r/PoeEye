using JetBrains.Annotations;
using PoeEye.Prism;
using PoeShared.Audio;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    [UsedImplicitly]
    internal sealed class PoeEyeTabListConfig : IPoeEyeConfigVersioned
    {
        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeTabConfig[] TabConfigs
        {
            get => tabConfigs;
            set => tabConfigs = value ?? new PoeEyeTabConfig[0];
        }

        public PoeEyeTabConfig DefaultConfig { get; set; } = new PoeEyeTabConfig
        {
            NotificationType = AudioNotificationType.Disabled,
            ApiModuleId = WellKnownApi.PathOfExileTradeApiModuleId
        };

        public int Version { get; set; } = 2;
    }
}