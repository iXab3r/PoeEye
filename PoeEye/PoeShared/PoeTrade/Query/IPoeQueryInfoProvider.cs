namespace PoeShared.PoeTrade.Query
{
    using Common;

    using JetBrains.Annotations;

    public interface IPoeQueryInfoProvider
    {
        string[] LeaguesList { [NotNull] get; }

        IPoeItemMod[] ModsList { [NotNull] get; }

        IPoeCurrency[] CurrenciesList { [NotNull] get; }

        IPoeItemType[] ItemTypes { [NotNull] get; }
    }
}