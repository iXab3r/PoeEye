namespace PoeShared.PoeTrade
{
    using Common;

    public interface IPoeQueryResult
    {
        IPoeItem[] ItemsList { get; }

        IPoeCurrency[] CurrenciesList { get; }

        IPoeItemMod[] ModsList { get; }
    }
}