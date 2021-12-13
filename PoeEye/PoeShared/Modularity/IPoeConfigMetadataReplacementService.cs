using System;

namespace PoeShared.Modularity
{
    public interface IPoeConfigMetadataReplacementService
    {
        PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata);

        void AddMetadataReplacement(string sourceTypeName, Type targetType);
    }
}