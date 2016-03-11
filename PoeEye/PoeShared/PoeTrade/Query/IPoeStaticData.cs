namespace PoeShared.PoeTrade.Query
{
    using Common;

    using JetBrains.Annotations;

    public interface IPoeStaticData
    {
        IPoeCurrency[] CurrenciesList { [NotNull] get; }

        IPoeItemMod[] ModsList { [NotNull] get; }

        string[] LeaguesList { [NotNull] get; }
    }
}