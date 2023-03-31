﻿using System.Reactive;
using System.Reflection;
using DynamicData;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.Modularity;

internal sealed class PoeConfigMetadataReplacementService : DisposableReactiveObjectWithLogger, IPoeConfigMetadataReplacementService
{
    private readonly SourceListEx<MetadataReplacement> replacementsSource = new();
    private readonly IObservableCache<MetadataReplacement, string> replacementsBySourceType;
    private readonly NamedLock substitutionsLock = new("ConfigMigrationServiceSubstitutions");

    public PoeConfigMetadataReplacementService(IAssemblyTracker assemblyTracker)
    {
        replacementsSource.Connect().BindToCollection(out var replacements).Subscribe().AddTo(Anchors);
        Replacements = replacements;

        replacementsBySourceType = replacementsSource
            .Connect()
            .AddKey(x => x.SourceTypeName)
            .AsObservableCache();

        this.WhenAnyValue(x => x.AutomaticallyLoadReplacements)
            .Select(x => x
                ? assemblyTracker.WhenLoaded.Distinct()
                : Observable.Empty<Assembly>())
            .Switch()
            .Subscribe(LoadMetadataReplacementsFromAssembly)
            .AddTo(Anchors);
    }

    public IReadOnlyObservableCollection<MetadataReplacement> Replacements { get; }

    public bool AutomaticallyLoadReplacements { get; set; } = true;

    public void Clear()
    {
        replacementsSource.Clear();
    }

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
        if (string.IsNullOrWhiteSpace(metadata.TypeName))
        {
            return metadata;
        }

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

    private void LoadMetadataReplacementsFromAssembly(Assembly assembly)
    {
        var logger = Log.WithSuffix(assembly.GetName().Name);
        logger.Debug(() => "Loading metadata replacements from assembly");

        try
        {
            var matchingTypes = assembly.GetTypes()
                .Where(x => !x.IsAbstract && typeof(IPoeConfigMetadataReplacementProvider).IsAssignableFrom(x))
                .ToArray();
            if (!matchingTypes.Any())
            {
                return;
            }
            logger.Debug(() => $"Detected replacements in assembly:\n\t{matchingTypes.DumpToTable()}");
            foreach (var providerType in matchingTypes)
            {
                logger.Debug(() => $"Creating new replacement provider: {providerType}");
                var provider = (IPoeConfigMetadataReplacementProvider)Activator.CreateInstance(providerType);
                var replacements = provider.Replacements.ToArray();
                logger.Debug(() => $"Received following replacements from provider {providerType}:\n\t{replacements.DumpToTable()}");
                replacements.ForEach(AddMetadataReplacement);
            }
        }
        catch (Exception e)
        {
            logger.Warn($"Failed to load metadata replacements from assembly {new { assembly, assembly.Location }}", e);
        }
    }

    public void AddMetadataReplacement(MetadataReplacement replacement)
    {
        Log.Debug(() => $"Registering metadata replacement: {replacement.SourceTypeName} => {replacement.TargetMetadata.TypeName}");
        replacementsSource.Add(replacement);
    }
    
    public PoeConfigMetadata AddMetadataReplacement(string sourceTypeName, Type targetType)
    {
        using var @lock = substitutionsLock.Enter();
        var metadata = new PoeConfigMetadata(targetType);
        AddMetadataReplacement(new MetadataReplacement {SourceTypeName = sourceTypeName, TargetMetadata = metadata});
        return metadata;
    }
}