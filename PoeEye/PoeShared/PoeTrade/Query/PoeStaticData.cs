namespace PoeShared.PoeTrade.Query
{
    using Common;

    public sealed class PoeStaticData : IPoeStaticData
    {
        public static readonly PoeStaticData Empty = new PoeStaticData()
        {
            CurrenciesList = new IPoeCurrency[]{ new PoeCurrency() { Name = "Currency list is empty"} },
            LeaguesList = new string[] { "League list is empty" },
            ItemTypes = new IPoeItemType[] { new PoeItemType() { Name = "Item type list is empty"} },
            ModsList = new IPoeItemMod[] { new PoeItemMod() { Name = "Mods list is empty"} },
            IsEmpty = true,
        };
        
        private IPoeCurrency[] currenciesList = new IPoeCurrency[0];
        private string[] leaguesList = new string[0];
        private IPoeItemMod[] modsList = new IPoeItemMod[0];
        private IPoeItemType[] itemsList = new IPoeItemType[0];

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

        public IPoeItemType[] ItemTypes
        {
            get { return itemsList; }
            set { itemsList = value ?? new IPoeItemType[0]; }
        }

        public string[] LeaguesList
        {
            get { return leaguesList; }
            set { leaguesList = value ?? new string[0]; }
        }

        public bool IsEmpty { get; private set; } = false;
    }
}