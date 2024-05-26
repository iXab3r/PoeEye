namespace PoeShared.Modularity;

internal interface IPoeConfigMetadataReplacementService : IPoeConfigMetadataReplacementRepository
{
    bool AutomaticallyLoadReplacements { get; set; }

    /// <summary>
    /// Looks up replacement metadata metadata, matched by type name
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="replacementMetadata"></param>
    /// <returns>True if replacement was found</returns>
    bool TryGetReplacement(PoeConfigMetadata metadata, out PoeConfigMetadata replacementMetadata);

    /// <summary>
    ///
    /// </summary>
    /// <param name="sourceTypeName"></param>
    /// <param name="targetType"></param>
    /// <returns>Metadata that will be returns</returns>
    PoeConfigMetadata AddMetadataReplacement(string sourceTypeName, Type targetType);
}