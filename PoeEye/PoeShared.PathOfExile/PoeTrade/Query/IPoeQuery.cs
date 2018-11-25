using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQuery
    {
        IPoeQueryArgument[] Arguments { [NotNull] get; }
    }
}