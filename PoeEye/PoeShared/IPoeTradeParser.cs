namespace PoeShared
{
    using Query;

    public interface IPoeTradeParser
    {
        IPoeQueryResult ParseQueryResult(string rawHtml);
    }
}