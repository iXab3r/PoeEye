namespace PoeShared.Modularity;

public sealed record MetadataReplacement
{
    public string SourceTypeName { get; set; }
            
    public PoeConfigMetadata TargetMetadata { get; set; }
}