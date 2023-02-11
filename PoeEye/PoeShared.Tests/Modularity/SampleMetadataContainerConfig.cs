using PoeShared.Modularity;

namespace PoeShared.Tests.Modularity;

public sealed record SampleMetadataContainerConfig : IPoeEyeConfig
{
    public PoeConfigMetadata Metadata { get; set; }
}