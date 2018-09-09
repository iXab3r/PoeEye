using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryResult
    {
        string Id { [CanBeNull] get; }

        IPoeItem[] ItemsList { [NotNull] get; }
    }
}