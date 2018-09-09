using JetBrains.Annotations;

namespace PoeShared.StashApi.ProcurementLegacy
{
    public interface IItemTypeAnalyzer
    {
        [CanBeNull]
        ItemTypeInfo ResolveTypeInfo([NotNull] string itemNameText);
    }
}