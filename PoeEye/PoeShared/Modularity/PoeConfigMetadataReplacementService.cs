using System;
using System.Collections.Generic;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;

namespace PoeShared.Modularity
{
    internal sealed class PoeConfigMetadataReplacementService : IPoeConfigMetadataReplacementService
    {
        private static readonly IFluentLog Log = typeof(PoeConfigMetadataReplacementService).PrepareLogger();

        private readonly Dictionary<string, PoeConfigMetadata> substituteMetadataByTypeName = new();
        private readonly NamedLock substitutionsLock = new("ConfigMigrationServiceSubstitutions"); 
        
        public PoeConfigMetadata ReplaceIfNeeded(PoeConfigMetadata metadata)
        {
            using var @lock = substitutionsLock.Enter();
            if (!substituteMetadataByTypeName.TryGetValue(metadata.TypeName, out var resolvedMetadata))
            {
                return metadata;
            }

            var replacement = metadata with
            {
                AssemblyName = resolvedMetadata.AssemblyName,
                TypeName = resolvedMetadata.TypeName
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
            substituteMetadataByTypeName[sourceTypeName] = metadata;
        }
    }
}