namespace PoeShared.Modularity;

public sealed record MetadataReplacement
{
    public string SourceTypeName { get; }
            
    public PoeConfigMetadata TargetMetadata { get; }

    public MetadataReplacement(string sourceTypeName, PoeConfigMetadata targetMetadata)
    {
        if (string.IsNullOrEmpty(sourceTypeName))
        {
            throw new ArgumentException("Source type name must be set");
        }
        SourceTypeName = sourceTypeName;
        TargetMetadata = targetMetadata ?? throw new ArgumentNullException(nameof(targetMetadata));
    }

    public static MetadataReplacement ForType(string sourceTypeName, Type targetType)
    {
        return new MetadataReplacement(sourceTypeName, new PoeConfigMetadata(targetType));
    }

    public static MetadataReplacement ForType<T>(string sourceTypeName) where T : class, IPoeEyeConfig
    {
        return new MetadataReplacement(sourceTypeName, new PoeConfigMetadata<T>());
    }
}