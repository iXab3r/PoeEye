namespace PoeEye.PoeTrade
{
    using System.IO;
    using System.Linq;
    using System.Web;

    using CsQuery;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeTradeParserModern : IPoeTradeParser
    {
        public IPoeQueryResult Parse(string rawHtml)
        {
            Guard.ArgumentNotNull(() => rawHtml);

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeQueryResult
            {
                CurrenciesList = ExtractCurrenciesList(parser),
                ItemsList = ExtractItems(parser),
                ModsList = ExtractModsList(parser),
                LeaguesList = ExtractLeaguesList(parser)
            };

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

        private bool IsValid(IPoeItem item)
        {
            return !string.IsNullOrWhiteSpace(item.ItemName);
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

        private string[] ExtractLeaguesList(CQ parser)
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

        private bool IsValid(IPoeItemMod mod)
        {
            return !string.IsNullOrWhiteSpace(mod.CodeName) && mod.ModType != PoeModType.Unknown;
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

        private static IPoeItem ParseItemRow(IDomObject row)
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
                Mods = implicitMods.Concat(explicitMods).ToArray(),
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
                ? (float?) null
                : result;
        }

        private static void TrimProperties(PoeItem item)
        {
            var propertiesToProcess = typeof (PoeItem)
                .GetProperties()
                .Where(x => x.PropertyType == typeof (string))
                .Where(x => x.CanRead && x.CanWrite)
                .ToArray();

            foreach (var propertyInfo in propertiesToProcess)
            {
                var currentValue = (string) propertyInfo.GetValue(item);
                var newValue = currentValue ?? string.Empty;
                newValue = HttpUtility.HtmlDecode(newValue);
                newValue = newValue.Trim();

                propertyInfo.SetValue(item, newValue);
            }
        }

        private static PoeItemRarity ExtractItemRarity(IDomObject row)
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

        private static IPoeLinksInfo ExtractLinksInfo(IDomObject row)
        {
            CQ parser = row.Render();

            var rawLinksText = parser["span[class=sockets-raw]"]?.Text();
            return string.IsNullOrWhiteSpace(rawLinksText) ? default(IPoeLinksInfo) : new PoeLinksInfo(rawLinksText);
        }

        private static IPoeItemMod[] ExtractExplicitMods(IDomObject row)
        {
            CQ parser = row.Render();
            return parser["ul[class=mods] li"].Select(x => ExtractItemMods(x, PoeModType.Explicit)).ToArray();
        }

        private static IPoeItemMod[] ExtractImplicitMods(IDomObject row)
        {
            CQ parser = row.Render();
            return parser["ul[class=mods withline] li"].Select(x => ExtractItemMods(x, PoeModType.Implicit)).ToArray();
        }

        private static IPoeItemMod ExtractItemMods(IDomObject itemModRow, PoeModType modType = PoeModType.Unknown)
        {
            CQ parser = itemModRow.Render();

            var result = new PoeItemMod
            {
                ModType = modType,
                CodeName = parser.Attr("data-name")?.Trim('#'),
                IsCrafted = parser.Select("u").Any(),
                Name = parser.Text()
            };

            return result;
        }
    }
}