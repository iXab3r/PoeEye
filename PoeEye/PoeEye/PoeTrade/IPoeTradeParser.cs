namespace PoeEye.PoeTrade
{
    using PoeShared;

    internal interface IPoeTradeParser
    {
        IPoeItem[] ParseQueryResult(string rawHtml);
    }
}