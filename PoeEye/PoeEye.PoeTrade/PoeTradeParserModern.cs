namespace PoeEye.PoeTrade
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Web;

    using CsQuery;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeTradeParserModern : IPoeTradeParser
    {
        private readonly IPoeTradeDateTimeExtractor dateTimeExtractor;

        public PoeTradeParserModern([NotNull] IPoeTradeDateTimeExtractor dateTimeExtractor)
        {
            Guard.ArgumentNotNull(() => dateTimeExtractor);

            this.dateTimeExtractor = dateTimeExtractor;
        }

        public IPoeQueryResult ParseQueryResponse(string rawHtml)
        {
            Guard.ArgumentNotNull(() => rawHtml);

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeQueryResult
            {
                ItemsList = ExtractItems(parser)
            };

            return result;
        }

        public IPoeStaticData ParseStaticData(string rawHtml)
        {
            Guard.ArgumentNotNull(() => rawHtml);

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeStaticData
            {
                ModsList = ExtractModsList(parser),
                LeaguesList = ExtractLeaguesList(parser),
                CurrenciesList = new IPoeCurrency[]
                {
                    new PoeCurrency {Name = "Blessed Orb", CodeName = KnownCurrencyNameList.BlessedOrb},
                    new PoeCurrency {Name = "Cartographer's Chisel", CodeName = KnownCurrencyNameList.CartographersChisel},
                    new PoeCurrency {Name = "Chaos Orb", CodeName = KnownCurrencyNameList.ChaosOrb},
                    new PoeCurrency {Name = "Chromatic Orb", CodeName = KnownCurrencyNameList.ChromaticOrb},
                    new PoeCurrency {Name = "Divine Orb", CodeName = KnownCurrencyNameList.DivineOrb},
                    new PoeCurrency {Name = "Exalted Orb", CodeName = KnownCurrencyNameList.ExaltedOrb},
                    new PoeCurrency {Name = "Gemcutter's Prism", CodeName = KnownCurrencyNameList.GemcuttersPrism},
                    new PoeCurrency {Name = "Jeweller's Orb", CodeName = KnownCurrencyNameList.JewellersOrb},
                    new PoeCurrency {Name = "Orb of Alchemy", CodeName = KnownCurrencyNameList.OrbOfAlchemy},
                    new PoeCurrency {Name = "Orb of Alteration", CodeName = KnownCurrencyNameList.OrbOfAlteration},
                    new PoeCurrency {Name = "Orb of Chance", CodeName = KnownCurrencyNameList.OrbOfChance},
                    new PoeCurrency {Name = "Orb of Fusing", CodeName = KnownCurrencyNameList.OrbOfFusing},
                    new PoeCurrency {Name = "Orb of Regret", CodeName = KnownCurrencyNameList.OrbOfRegret},
                    new PoeCurrency {Name = "Orb of Scouring", CodeName = KnownCurrencyNameList.OrbOfScouring},
                    new PoeCurrency {Name = "Regal Orb", CodeName = KnownCurrencyNameList.RegalOrb},
                    new PoeCurrency {Name = "Vaal Orb", CodeName = KnownCurrencyNameList.VaalOrb}
                },
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
                }
            };
            return result;
        }

        private IPoeItemMod[] ExtractModsList(CQ parser)
        {
            var allModRows = parser["div[class='row explicit'] select option"].ToList();

            var allMods = allModRows
                .Select(ParseItemModRow)
                .Where(IsValid)
                .Distinct(PoeItemMod.CodeNameComparer)
                .Cast<IPoeItemMod>()
                .ToArray();

            return allMods;
        }

        private static string[] ExtractLeaguesList(CQ parser)
        {
            var leaguesRows = parser["select[name=league] option"].ToList();
            var leaguesList = leaguesRows
                .Select(ParseLeagueRow)
                .Where(IsValid)
                .Distinct()
                .ToArray();
            return leaguesList;
        }

        private PoeItemMod ParseItemModRow(IDomObject row)
        {
            var result = new PoeItemMod
            {
                Name = row.InnerText,
                CodeName = row["value"]
            };

            var isImplicit = result.CodeName?.Contains("(implicit)");
            result.ModType = isImplicit != null && isImplicit.Value
                ? PoeModType.Implicit
                : PoeModType.Explicit;

            return result;
        }

        private static IPoeCurrency ParseCurrencyRow(IDomObject row)
        {
            var result = new PoeCurrency();
            CQ parser = row.Render();
            result.Name = parser.Text();
            result.CodeName = parser.Attr("value");
            return result;
        }

        private static string ParseLeagueRow(IDomObject row)
        {
            CQ parser = row.Render();
            var result = parser.Attr("value");
            return result;
        }

        private static bool IsValid(IPoeCurrency currency)
        {
            return !string.IsNullOrWhiteSpace(currency.CodeName);
        }

        private static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        private IPoeItem ParseItemRow(IDomObject row)
        {
            CQ parser = row.Render();

            var implicitMods = ExtractImplicitMods(row);
            var explicitMods = ExtractExplicitMods(row);
            var result = new PoeItem
            {
                ItemIconUri = parser["div[class=icon] img"]?.Attr("src"),
                ItemName = parser.Attr("data-name"),
                TradeForumUri = parser["td[class=item-cell] a[class^=title]"]?.Attr("href"),
                UserForumName = parser.Attr("data-seller"),
                UserIgn = parser.Attr("data-ign"),
                UserIsOnline = parser["tr[class=bottom-row] span[class~=success]"].Any(),
                Price = parser.Attr("data-buyout"),
                League = parser.Attr("data-league"),
                Hash = parser["span[class=click-button]"]?.Attr("data-hash"),
                ThreadId = parser["span[class=click-button]"]?.Attr("data-thread"),
                Note = parser["span[class=item-note]"]?.Text(),
                Quality = parser["td[class=table-stats] td[data-name=q]"]?.Text(),
                Physical = parser["td[class=table-stats] td[data-name=quality_pd]"]?.Text(),
                Elemental = parser["td[class=table-stats] td[data-name=ed]"]?.Text(),
                AttacksPerSecond = parser["td[class=table-stats] td[data-name=aps]"]?.Text(),
                DamagePerSecond = parser["td[class=table-stats] td[data-name=quality_dps]"]?.Text(),
                PhysicalDamagePerSecond = parser["td[class=table-stats] td[data-name=quality_pdps]"]?.Text(),
                ElementalDamagePerSecond = parser["td[class=table-stats] td[data-name=edps]"]?.Text(),
                Armour = parser["td[class=table-stats] td[data-name=quality_armour]"]?.Text(),
                Evasion = parser["td[class=table-stats] td[data-name=quality_evasion]"]?.Text(),
                Shield = parser["td[class=table-stats] td[data-name=quality_shield]"]?.Text(),
                BlockChance = parser["td[class=table-stats] td[data-name=block]"]?.Text(),
                CriticalChance = parser["td[class=table-stats] td[data-name=crit]"]?.Text(),
                Level = parser["td[class=table-stats] td[data-name=level]"]?.Text(),
                Requirements = parser["td[class=item-cell] ul[class=requirements proplist]"]?.Text(),
                IsCorrupted = parser["td[class=item-cell] span[class~=corrupted]"].Any(),
                FirstSeen = dateTimeExtractor.ExtractTimestamp(parser["td[class=item-cell] span[class~=found-time-ago]"]?.Text()),
                Mods = implicitMods.Concat(explicitMods).Where(IsValid).ToArray(),
                Links = ExtractLinksInfo(row),
                Rarity = ExtractItemRarity(row)
            };
            TrimProperties(result);
            return result;
        }

        private static float? ParseFloat(string rawValue)
        {
            float result;
            return !float.TryParse(rawValue, out result)
                ? (float?)null
                : result;
        }

        private static void TrimProperties<T>(T source)
        {
            var propertiesToProcess = typeof(T)
                .GetProperties()
                .Where(x => x.PropertyType == typeof(string))
                .Where(x => x.CanRead && x.CanWrite)
                .ToArray();

            foreach (var propertyInfo in propertiesToProcess)
            {
                var currentValue = (string)propertyInfo.GetValue(source);
                var newValue = currentValue ?? string.Empty;
                newValue = HttpUtility.HtmlDecode(newValue);
                newValue = newValue.Replace("\n", string.Empty);
                newValue = newValue.Replace("\r", string.Empty);
                newValue = newValue.Trim();

                propertyInfo.SetValue(source, newValue);
            }
        }

        private PoeItemRarity ExtractItemRarity(IDomObject row)
        {
            CQ parser = row.Render();

            var titleClass = parser["td[class=item-cell] a[class^=title]"]?.Attr("class");
            switch (titleClass)
            {
                case "title itemframe0":
                    return PoeItemRarity.Normal;
                case "title itemframe1":
                    return PoeItemRarity.Magic;
                case "title itemframe2":
                    return PoeItemRarity.Rare;
                case "title itemframe3":
                    return PoeItemRarity.Unique;
                default:
                    return PoeItemRarity.Unknown;
            }
        }

        private IPoeLinksInfo ExtractLinksInfo(IDomObject row)
        {
            CQ parser = row.Render();

            var rawLinksText = parser["span[class=sockets-raw]"]?.Text();
            return string.IsNullOrWhiteSpace(rawLinksText) ? default(IPoeLinksInfo) : new PoeLinksInfo(rawLinksText);
        }

        private IPoeItemMod[] ExtractExplicitMods(IDomObject row)
        {
            CQ parser = row.Render();
            return parser["ul[class=mods] li"].Select(x => ExtractItemMods(x, PoeModType.Explicit)).ToArray();
        }

        private IPoeItemMod[] ExtractImplicitMods(IDomObject row)
        {
            CQ parser = row.Render();
            return parser["ul[class=mods withline] li"].Select(x => ExtractItemMods(x, PoeModType.Implicit)).ToArray();
        }

        private IPoeItemMod ExtractItemMods(IDomObject itemModRow, PoeModType modType = PoeModType.Unknown)
        {
            CQ parser = itemModRow.Render();

            var result = new PoeItemMod
            {
                ModType = modType,
                CodeName = parser.Attr("data-name")?.Trim('#'),
                IsCrafted = parser.Select("u").Any(),
                Name = parser["li"]?.Text(),
            };

            TrimProperties(result);
            return result;
        }

        private IPoeItem[] ExtractItems(CQ parser)
        {
            var rows = parser["tbody[id^=item-container-]"];

            var items = rows
                .Select(ParseItemRow)
                .Where(IsValid)
                .ToArray();
            return items;
        }

        private IPoeCurrency[] ExtractCurrenciesList(CQ parser)
        {
            var currenciesRows = parser["select[name=buyout_currency] option"].ToList();

            var currencies = currenciesRows
                .Select(ParseCurrencyRow)
                .Where(IsValid)
                .ToArray();
            return currencies;
        }

        private static bool IsValid(IPoeItem item)
        {
            return !string.IsNullOrWhiteSpace(item.ItemName);
        }

        private static bool IsValid(IPoeItemMod mod)
        {
            return !string.IsNullOrWhiteSpace(mod.CodeName) && !string.IsNullOrWhiteSpace(mod.Name) && mod.ModType != PoeModType.Unknown;
        }
    }
}