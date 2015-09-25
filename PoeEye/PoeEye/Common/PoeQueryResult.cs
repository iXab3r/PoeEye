namespace PoeEye.Common
{
    using PoeShared;
    using PoeShared.Query;

    [ToString]
    internal sealed class PoeQueryResult : IPoeQueryResult
    {
        public IPoeItem[] ItemsList { get; set; }

        public IPoeCurrency[] CurrenciesList { get; set; }

        public IPoeItemMod[] ModsList { get; set; }
    }
}