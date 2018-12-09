using JetBrains.Annotations;

namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryArgument
    {
        string Name { [NotNull] get; }
    }
}