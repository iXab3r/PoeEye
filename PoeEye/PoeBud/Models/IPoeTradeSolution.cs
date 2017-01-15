using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IPoeTradeSolution
    {
        IPoeTradeItem[] Items { [NotNull] get; }

        ITab[] Tabs { [NotNull] get; }
    }
}