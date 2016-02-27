namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public interface IPoeQuery
    {
        IPoeQueryArgument[] Arguments { [NotNull] get; }
    }
}