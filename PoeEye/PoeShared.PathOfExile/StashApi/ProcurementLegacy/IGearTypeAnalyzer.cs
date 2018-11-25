using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;

namespace PoeShared.StashApi.ProcurementLegacy
{
    internal interface IGearTypeAnalyzer
    {
        GearType Resolve([NotNull] string itemType);
    }
}