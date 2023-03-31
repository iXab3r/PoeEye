namespace PoeShared.Modularity;

public interface IPoeConfigMetadataReplacementRepository
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
}