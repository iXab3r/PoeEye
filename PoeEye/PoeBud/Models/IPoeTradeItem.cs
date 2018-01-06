using PoeShared.Common;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IPoeTradeItem
    {
        string Name { [NotNull] get; }

        ItemPosition Position { get; }

        int TabIndex { get; }

        GearType ItemType { get; }
    }
}