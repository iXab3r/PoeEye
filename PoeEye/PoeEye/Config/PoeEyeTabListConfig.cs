using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeTabListConfig : IPoeEyeConfigVersioned
    {
        private PoeEyeTabConfig[] tabConfigs = new PoeEyeTabConfig[0];

        public PoeEyeTabConfig[] TabConfigs
        {
            get => tabConfigs;
            set => tabConfigs = value ?? new PoeEyeTabConfig[0];
        }

        public int Version { get; set; } = 1;
    }
}