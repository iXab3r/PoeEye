namespace PoeShared.PoeTrade
{
    public interface IPoeTradeParser
    {
        IPoeQueryResult ParseQueryResult(string rawHtml);
    }
}