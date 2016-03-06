﻿namespace PoeEye.PoeTrade
{
    using System;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeQueryInfoProvider : IPoeQueryInfoProvider
    {
        private readonly Lazy<IPoeQueryResult> lazyDataLoader;
        private readonly IPoeApi poeApi;

        public PoeQueryInfoProvider([NotNull] IPoeApi poeApi)
        {
            Guard.ArgumentNotNull(() => poeApi);
            this.poeApi = poeApi;

            lazyDataLoader = new Lazy<IPoeQueryResult>(RefreshData);

            CurrenciesList = new IPoeCurrency[]
            {
                new PoeCurrency {Name = "Blessed Orb", CodeName = "blessed"},
                new PoeCurrency {Name = "Cartographer's Chisel", CodeName = "chisel"},
                new PoeCurrency {Name = "Chaos Orb", CodeName = "chaos"},
                new PoeCurrency {Name = "Chromatic Orb", CodeName = "chromatic"},
                new PoeCurrency {Name = "Divine Orb", CodeName = "divine"},
                new PoeCurrency {Name = "Exalted Orb", CodeName = "exalted"},
                new PoeCurrency {Name = "Gemcutter's Prism", CodeName = "gcp"},
                new PoeCurrency {Name = "Jeweller's Orb", CodeName = "jewellers"},
                new PoeCurrency {Name = "Orb of Alchemy", CodeName = "alchemy"},
                new PoeCurrency {Name = "Orb of Alteration", CodeName = "alteration"},
                new PoeCurrency {Name = "Orb of Chance", CodeName = "chance"},
                new PoeCurrency {Name = "Orb of Fusing", CodeName = "fusing"},
                new PoeCurrency {Name = "Orb of Regret", CodeName = "regret"},
                new PoeCurrency {Name = "Orb of Scouring", CodeName = "scouring"},
                new PoeCurrency {Name = "Regal Orb", CodeName = "regal"}
            };

            ItemTypes = new IPoeItemType[]
            {
                new PoeItemType {Name = "Generic One-Handed Weapon", CodeName = "1h"},
                new PoeItemType {Name = "Generic Two-Handed Weapon", CodeName = "2h"},
                new PoeItemType {Name = "Bow", CodeName = "Bow"},
                new PoeItemType {Name = "Claw", CodeName = "Claw"},
                new PoeItemType {Name = "Dagger", CodeName = "Dagger"},
                new PoeItemType {Name = "One Hand Axe", CodeName = "One Hand Axe"},
                new PoeItemType {Name = "One Hand Mace", CodeName = "One Hand Mace"},
                new PoeItemType {Name = "One Hand Sword", CodeName = "One Hand Sword"},
                new PoeItemType {Name = "Sceptre", CodeName = "Sceptre"},
                new PoeItemType {Name = "Staff", CodeName = "Staff"},
                new PoeItemType {Name = "Two Hand Axe", CodeName = "Two Hand Axe"},
                new PoeItemType {Name = "Two Hand Mace", CodeName = "Two Hand Mace"},
                new PoeItemType {Name = "Two Hand Sword", CodeName = "Two Hand Sword"},
                new PoeItemType {Name = "Wand", CodeName = "Wand"},
                new PoeItemType {Name = "Body Armour", CodeName = "Body Armour"},
                new PoeItemType {Name = "Boots", CodeName = "Boots"},
                new PoeItemType {Name = "Gloves", CodeName = "Gloves"},
                new PoeItemType {Name = "Helmet", CodeName = "Helmet"},
                new PoeItemType {Name = "Shield", CodeName = "Shield"},
                new PoeItemType {Name = "Amulet", CodeName = "Amulet"},
                new PoeItemType {Name = "Belt", CodeName = "Belt"},
                new PoeItemType {Name = "Currency", CodeName = "Currency"},
                new PoeItemType {Name = "Divination Card", CodeName = "Divination Card"},
                new PoeItemType {Name = "Fishing Rods", CodeName = "Fishing Rods"},
                new PoeItemType {Name = "Flask", CodeName = "Flask"},
                new PoeItemType {Name = "Gem", CodeName = "Gem"},
                new PoeItemType {Name = "Jewel", CodeName = "Jewel"},
                new PoeItemType {Name = "Map", CodeName = "Map"},
                new PoeItemType {Name = "Quiver", CodeName = "Quiver"},
                new PoeItemType {Name = "Ring", CodeName = "Ring"},
                new PoeItemType {Name = "Vaal Fragments", CodeName = "Vaal Fragments"}
            };
        }

        public string[] LeaguesList => lazyDataLoader.Value.LeaguesList;

        public IPoeItemMod[] ModsList => lazyDataLoader.Value.ModsList;

        public IPoeCurrency[] CurrenciesList { get; }

        public IPoeItemType[] ItemTypes { get; }

        private IPoeQueryResult RefreshData()
        {
            try
            {
                var queryResult = poeApi.GetStaticData().Result;
                return queryResult;
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                return new PoeQueryResult();
            }
        }
    }
}