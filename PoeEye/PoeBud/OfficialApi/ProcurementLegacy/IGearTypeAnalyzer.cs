using JetBrains.Annotations;
using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.OfficialApi.ProcurementLegacy
{
    internal interface IGearTypeAnalyzer
    {
        GearType Resolve([NotNull] string itemType);
    }
}