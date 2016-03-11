namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    using Query;

    public interface IPoeTradeParser
    {
        [NotNull]
        IPoeQueryResult ParseQueryResponse([NotNull] string rawHtml);

        [NotNull]
        IPoeStaticData ParseStaticData([NotNull] string rawHtml);
    }
}