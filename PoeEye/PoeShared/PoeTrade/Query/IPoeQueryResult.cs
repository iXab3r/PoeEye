namespace PoeShared.PoeTrade.Query
{
    using Common;

    using JetBrains.Annotations;

    public interface IPoeQueryResult
    {
        IPoeItem[] ItemsList { [NotNull] get; }

        IPoeCurrency[] CurrenciesList { [NotNull] get; }

        IPoeItemMod[] ModsList { [NotNull] get; }
    }
}