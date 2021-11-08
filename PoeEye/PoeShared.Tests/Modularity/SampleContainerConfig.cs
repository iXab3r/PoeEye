using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity
{
    public sealed class SampleContainerConfig : IPoeEyeConfig
    {
        public IPoeEyeConfig InnerConfig { get; set; }
    }
}