namespace PoeShared.PoeTrade.Query
{
    using Common;

    using JetBrains.Annotations;

    public interface IPoeQueryResult
    {
        string Id { [CanBeNull] get; }

        IPoeItem[] ItemsList { [NotNull] get; }
    }
}