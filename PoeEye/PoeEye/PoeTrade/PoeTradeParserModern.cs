namespace PoeEye.PoeTrade
{
    using System.IO;
    using System.Linq;

    using Common;

    using CsQuery;

    using PoeShared;

    internal sealed class PoeTradeParserModern : IPoeTradeParser
    {
        public IPoeItem[] ParseQueryResult(string rawHtml)
        {
            var parser = new CQ(new StringReader(rawHtml));

            var rows = parser["tbody[id^=item-container-]"];

            var items = rows.Select(ParseRow);

            return items.ToArray();
        }

        private static IPoeItem ParseRow(IDomObject row)
        {
            var result = new PoeItem();

            CQ parser = row.Render();

            result.ItemIconUri = parser["div[class=icon] img"].Attr("src");
            result.ItemName = parser.Attr("data-name");
            result.TradeForumUri = parser["td[class=item-cell] a[class^=title]"].Attr("href");
            result.UserForumName = parser.Attr("data-seller");
            result.UserIgn = parser.Attr("data-ign");
            result.Price = parser.Attr("data-buyout");
            result.League = parser.Attr("data-league");

            return result;
        }
    }
}