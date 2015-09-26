namespace PoeEye.PoeTrade
{
    using System.IO;
    using System.Linq;

    using CsQuery;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;

    internal sealed class PoeTradeParserModern : IPoeTradeParser
    {
        public IPoeQueryResult ParseQueryResult(string rawHtml)
        {
            Guard.ArgumentNotNull(() => rawHtml);

            var parser = new CQ(new StringReader(rawHtml));

            var result = new PoeQueryResult
            {
                CurrenciesList = ExtractCurrenciesList(parser),
                ItemsList = ExtractItems(parser),
                ModsList = ExtractModsList(parser)
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
            var implicitModsRows = parser["select[name=impl] option"].ToList();
            var implicitMods = implicitModsRows.Select(x => ParseItemModRow(x, PoeModType.Implicit)).ToArray();

            var explicitModsRows = parser["select[name=mods] option"].ToList();
            var explicitMods = explicitModsRows.Select(x => ParseItemModRow(x, PoeModType.Explicit)).ToArray();

            var allMods = implicitMods
                .Concat(explicitMods)
                .Where(IsValid)
                .ToArray();

            return allMods;
        }

        private IPoeItemMod ParseItemModRow(IDomObject row, PoeModType modType)
        {
            var result = new PoeItemMod
            {
                ModType = modType,
                Name = row.InnerText,
                CodeName = row["value"]
            };

            return result;
        }

        private bool IsValid(IPoeItemMod mod)
        {
            return !string.IsNullOrWhiteSpace(mod.CodeName);
        }

        private static IPoeCurrency ParseCurrencyRow(IDomObject row)
        {
            var result = new PoeCurrency();
            CQ parser = row.Render();
            result.Name = parser.Text();
            result.CodeName = parser.Attr("value");
            return result;
        }

        private static bool IsValid(IPoeCurrency currency)
        {
            return !string.IsNullOrWhiteSpace(currency.CodeName);
        }

        private static IPoeItem ParseItemRow(IDomObject row)
        {
            CQ parser = row.Render();

            var result = new PoeItem
            {
                ItemIconUri = parser["div[class=icon] img"].Attr("src"),
                ItemName = parser.Attr("data-name"),
                TradeForumUri = parser["td[class=item-cell] a[class^=title]"].Attr("href"),
                UserForumName = parser.Attr("data-seller"),
                UserIgn = parser.Attr("data-ign"),
                Price = parser.Attr("data-buyout"),
                League = parser.Attr("data-league"),
                Mods = parser["ul[class=mods] li"].Select(ExtractItemMods).ToArray()
            };

            return result;
        }

        private static IPoeItemMod ExtractItemMods(IDomObject itemModRow)
        {
            CQ parser = itemModRow.Render();

            var result = new PoeItemMod
            {
                ModType = PoeModType.Unknown,
                CodeName = parser.Attr("data-name")?.Trim('#'),
                Name = parser.Text()
            };

            return result;
        }
    }
}