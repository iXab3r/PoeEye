namespace PoeShared.PoeTrade.Query
{
    using Common;

    using JetBrains.Annotations;

    public interface IPoeQueryResult
    {
        IPoeItem[] ItemsList { [NotNull] get; }
    }
}