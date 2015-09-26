namespace PoeEye.PoeTrade
{
    using PoeShared.Common;
    using PoeShared.PoeTrade;

    [ToString]
    internal sealed class PoeQueryResult : IPoeQueryResult
    {
        public IPoeItem[] ItemsList { get; set; }

        public IPoeCurrency[] CurrenciesList { get; set; }

        public IPoeItemMod[] ModsList { get; set; }
    }
}