namespace PoeShared.Query
{
    public interface IPoeQueryResult
    {
        IPoeItem[] ItemsList { get; }

        IPoeCurrency[] CurrenciesList { get; }

        IPoeItemMod[] ModsList { get; }
    }
}