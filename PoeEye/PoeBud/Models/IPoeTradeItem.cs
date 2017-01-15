using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IPoeTradeItem
    {
        string Name { [NotNull] get; }

        int X { get; }

        int Y { get; }

        int TabIndex { get; }

        GearType ItemType { get; }
    }
}