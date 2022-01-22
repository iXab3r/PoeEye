using System;

namespace PoeShared.Modularity;

public interface IPoeConfigMetadataReplacementService
{
    IObservable<PoeConfigMetadata> Watch(PoeConfigMetadata metadata);

    PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata);

    void AddMetadataReplacement(string sourceTypeName, Type targetType);
}