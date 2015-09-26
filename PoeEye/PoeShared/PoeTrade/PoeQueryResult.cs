namespace PoeShared.PoeTrade
{
    using Common;

    public sealed class PoeQueryResult : IPoeQueryResult
    {
        private IPoeCurrency[] currenciesList = new IPoeCurrency[0];
        private IPoeItem[] itemsList = new IPoeItem[0];
        private IPoeItemMod[] modsList = new IPoeItemMod[0];

        public IPoeItem[] ItemsList
        {
            get { return itemsList; }
            set { itemsList = value ?? new IPoeItem[0]; }
        }

        public IPoeCurrency[] CurrenciesList
        {
            get { return currenciesList; }
            set { currenciesList = value ?? new IPoeCurrency[0]; }
        }

        public IPoeItemMod[] ModsList
        {
            get { return modsList; }
            set { modsList = value ?? new IPoeItemMod[0]; }
        }
    }
}