namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    using Query;

    public interface IPoeTradeParser
    {
        [NotNull] 
        IPoeQueryResult ParseQueryResult([NotNull] string rawHtml);
    }
}