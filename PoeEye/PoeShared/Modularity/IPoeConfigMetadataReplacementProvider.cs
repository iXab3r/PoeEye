namespace PoeShared.Modularity;

public interface IPoeConfigMetadataReplacementProvider
{
    IEnumerable<MetadataReplacement> Replacements { get; }
}