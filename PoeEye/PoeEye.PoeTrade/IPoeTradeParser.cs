using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PoeTrade
{
    internal interface IPoeTradeParser
    {
        [NotNull]
        IPoeQueryResult ParseQueryResponse([NotNull] string rawHtml);

        [NotNull]
        IPoeStaticData ParseStaticData([NotNull] string rawHtml);
    }
}