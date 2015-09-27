namespace PoeShared.PoeTrade.Query
{
    using JetBrains.Annotations;

    public interface IPoeQueryArgument
    {
        string Name { [NotNull] get; }
    }
}