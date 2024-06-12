namespace PoeShared.Modularity;

public sealed record MetadataReplacement
{
    public string SourceTypeName { get; set; }
            
    public PoeConfigMetadata TargetMetadata { get; set; }

    public static MetadataReplacement ForType<T>(string sourceTypeName) where T : class, IPoeEyeConfig
    {
        return new MetadataReplacement()
        {
            SourceTypeName = sourceTypeName,
            TargetMetadata = new PoeConfigMetadata<T>()
        };
    }
}