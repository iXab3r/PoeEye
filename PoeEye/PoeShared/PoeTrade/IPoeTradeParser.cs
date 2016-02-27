namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    using Query;

    public interface IPoeTradeParser
    {
        [NotNull]
        IPoeQueryResult Parse([NotNull] string rawHtml);
    }
}