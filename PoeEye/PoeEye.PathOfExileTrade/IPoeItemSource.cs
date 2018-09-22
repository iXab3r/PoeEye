using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.PoeTrade.Query;

namespace PoeEye.PathOfExileTrade
{
    internal interface IPoeItemSource
    {
        [NotNull]
        Task<IPoeQueryResult> FetchItems([NotNull] IPoeQueryResult initial, [NotNull] IReadOnlyList<string> itemIds);
    }
}