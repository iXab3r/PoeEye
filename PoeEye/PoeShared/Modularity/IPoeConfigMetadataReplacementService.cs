namespace PoeShared.Modularity;

internal interface IPoeConfigMetadataReplacementService : IPoeConfigMetadataReplacementRepository
{
    bool AutomaticallyLoadReplacements { get; set; }

    /// <summary>
    /// Replaces metadata(matched by type name) if replacement is registered
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata);

    /// <summary>
    ///
    /// </summary>
    /// <param name="sourceTypeName"></param>
    /// <param name="targetType"></param>
    /// <returns>Metadata that will be returns</returns>
    PoeConfigMetadata AddMetadataReplacement(string sourceTypeName, Type targetType);
}