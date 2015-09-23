namespace PoeEye.PoeTrade
{
    using System.IO;
    using System.Linq;

    using Common;

    using CsQuery;

    using PoeShared;

    internal sealed class PoeTradeParserAncientWhite : IPoeTradeParser
    {
        public IPoeItem[] ParseQueryResult(string rawHtml)
        {
            var parser = new CQ(new StringReader(rawHtml));

            var rows = parser["#results tbody tr"];

            var items = rows.Select(ParseRow);

            return items.ToArray();
        }

        private IPoeItem ParseRow(IDomObject row)
        {
            var result = new PoeItem();

            CQ parser = row.Render();

            result.ItemIconUri = parser[".item-icon img"].Attr("src");
            result.ItemName = parser["td a[class~=itemlink]"].Text();
            result.TradeForumUri = parser["td a[class~=itemlink]"].Attr("href");
            result.UserForumUri = parser["td div span[class=user] a"].Attr("href");
            result.UserForumName = parser["td div span[class=user] a"].Text();
            result.Price = parser["td span[class~=tooltip-s]"].Attr("title");

            return result;
        }
    }
}