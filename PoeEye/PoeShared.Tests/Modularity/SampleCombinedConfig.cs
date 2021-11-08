using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity
{
    public sealed class SampleCombinedConfig : IPoeEyeConfig
    {
        public IPoeEyeConfig[] Configs { get; set; }
    }
}