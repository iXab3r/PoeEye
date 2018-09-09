using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeStaticData
    {
        IPoeCurrency[] CurrenciesList { [NotNull] get; }

        IPoeItemMod[] ModsList { [NotNull] get; }

        IPoeItemType[] ItemTypes { [NotNull] get; }

        string[] LeaguesList { [NotNull] get; }
        
        bool IsEmpty { get; }
    }
}