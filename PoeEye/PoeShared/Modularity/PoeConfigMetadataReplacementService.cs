using DynamicData;
using PoeShared.Services;

namespace PoeShared.Modularity;

internal sealed class PoeConfigMetadataReplacementService : IPoeConfigMetadataReplacementService
{
    private static readonly IFluentLog Log = typeof(PoeConfigMetadataReplacementService).PrepareLogger();

    private readonly SourceCache<MetadataReplacementKey, string> substituteMetadataByTypeName = new(x => x.SourceTypeName);
    private readonly NamedLock substitutionsLock = new("ConfigMigrationServiceSubstitutions");

    public IObservable<PoeConfigMetadata> Watch(PoeConfigMetadata metadata)
    {
        if (substituteMetadataByTypeName.TryGetValue(metadata.TypeName, out var resolvedMetadata))
        {
            return Observable.Return(resolvedMetadata.TargetMetadata);
        }
        return substituteMetadataByTypeName.WatchCurrentValue(metadata.TypeName).Select(x => x.TargetMetadata).StartWith(metadata);
    }

    public PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata)
    {
        using var @lock = substitutionsLock.Enter();
        if (!substituteMetadataByTypeName.TryGetValue(metadata.TypeName, out var resolvedMetadata))
        {
            return metadata;
        }

        var replacement = metadata with
        {
            AssemblyName = resolvedMetadata.TargetMetadata.AssemblyName,
            TypeName = resolvedMetadata.TargetMetadata.TypeName
        };
        Log.Debug(() => $"Replacing metadata {metadata} with {replacement}");
        return replacement;
    }

    public void AddMetadataReplacement(string sourceTypeName, Type targetType)
    {
        using var @lock = substitutionsLock.Enter();
        var metadata = new PoeConfigMetadata
        {
            AssemblyName = targetType.Assembly.GetName().Name,
            TypeName = targetType.FullName
        };
        Log.Debug(() => $"Registering replacement: {sourceTypeName} => {metadata}");
        substituteMetadataByTypeName.AddOrUpdate(new MetadataReplacementKey { SourceTypeName = sourceTypeName, TargetMetadata = metadata });
    }


    private sealed record MetadataReplacementKey
    {
        public string SourceTypeName { get; set; }
            
        public PoeConfigMetadata TargetMetadata { get; set; }
    }
}