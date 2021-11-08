using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity
{
    public sealed class SampleVersionedConfig : IPoeEyeConfigVersioned
    {
        public string Value { get; set; } = "Version#2";

        public int Version { get; set; } = 2;
    }
}