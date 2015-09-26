namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    public interface IPoeTradeParser
    {
        [NotNull] 
        IPoeQueryResult ParseQueryResult([NotNull] string rawHtml);
    }
}