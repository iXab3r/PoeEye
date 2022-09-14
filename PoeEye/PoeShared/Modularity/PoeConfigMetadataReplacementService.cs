using DynamicData;
using PoeShared.Services;

namespace PoeShared.Modularity;

internal sealed class PoeConfigMetadataReplacementService : DisposableReactiveObjectWithLogger, IPoeConfigMetadataReplacementService
{
    private readonly SourceListEx<MetadataReplacement> replacementsSource = new();
    private readonly IObservableCache<MetadataReplacement, string> replacementsBySourceType;
    private readonly NamedLock substitutionsLock = new("ConfigMigrationServiceSubstitutions");
    
    public PoeConfigMetadataReplacementService()
    {
        replacementsSource.Connect().BindToCollection(out var replacements).Subscribe().AddTo(Anchors);
        Replacements = replacements;

        replacementsBySourceType = replacementsSource
            .Connect()
            .AddKey(x => x.SourceTypeName)
            .AsObservableCache();
    }

    public IReadOnlyObservableCollection<MetadataReplacement> Replacements { get; }

    public IObservable<string> WatchForAddedReplacements(Type targetType)
    {
        return Observable.Create<string>(observer =>
        {
            var metadata = new PoeConfigMetadata(targetType);
            
            observer.OnNext(metadata.TypeName);

            return replacementsSource.Connect()
                .Filter(x => x.TargetMetadata == metadata)
                .OnItemAdded(replacement => observer.OnNext(replacement.SourceTypeName))
                .StartWith()
                .Subscribe();
        });
    }

    public IObservable<PoeConfigMetadata> Watch(PoeConfigMetadata metadata)
    {
        if (replacementsBySourceType.TryGetValue(metadata.TypeName, out var resolvedMetadata))
        {
            return Observable.Return(resolvedMetadata.TargetMetadata);
        }
        return replacementsBySourceType
            .WatchCurrentValue(metadata.TypeName)
            .Where(x => x != null)
            .Select(x => x.TargetMetadata)
            .StartWith(metadata);
    }

    public PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata)
    {
        using var @lock = substitutionsLock.Enter();
        if (!replacementsBySourceType.TryGetValue(metadata.TypeName, out var resolvedMetadata))
        {
            return metadata;
        }

        var replacement = metadata with
        {
            AssemblyName = resolvedMetadata.TargetMetadata.AssemblyName,
            TypeName = resolvedMetadata.TargetMetadata.TypeName
        };
        Log.Debug(() => $"Replacing legacy metadata: {metadata} => {replacement}");
        return replacement;
    }

    public PoeConfigMetadata AddMetadataReplacement(string sourceTypeName, Type targetType)
    {
        using var @lock = substitutionsLock.Enter();
        var metadata = new PoeConfigMetadata(targetType);
        Log.Debug(() => $"Registering metadata replacement: {sourceTypeName} => {metadata}");
        replacementsSource.Add(new MetadataReplacement { SourceTypeName = sourceTypeName, TargetMetadata = metadata });
        return metadata;
    }
}