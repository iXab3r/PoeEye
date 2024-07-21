using System.Collections.Generic;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Squirrel.Updater;

namespace PoeShared.Squirrel.Prism;

[UsedImplicitly]
public sealed class MetadataReplacements : IPoeConfigMetadataReplacementProvider
{
    public IEnumerable<MetadataReplacement> Replacements { get; } = new[]
    {
        MetadataReplacement.ForType<UpdateSettingsConfig>("PoeShared.Squirrel.UpdateSettingsConfig") 
    };
}