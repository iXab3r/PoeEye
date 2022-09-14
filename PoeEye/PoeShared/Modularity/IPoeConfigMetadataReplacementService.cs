namespace PoeShared.Modularity;

public interface IPoeConfigMetadataReplacementService
{
    IReadOnlyObservableCollection<MetadataReplacement> Replacements { get; }

    /// <summary>
    /// Returns stream of names of types that will be replaced by target type
    /// </summary>
    /// <param name="targetType"></param>
    /// <returns></returns>
    IObservable<string> WatchForAddedReplacements(Type targetType);
    
    /// <summary>
    /// Returns a stream of updates for metadata of a specific type
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    IObservable<PoeConfigMetadata> Watch(PoeConfigMetadata metadata);

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