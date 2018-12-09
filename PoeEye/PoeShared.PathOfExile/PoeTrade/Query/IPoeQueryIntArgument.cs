namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryIntArgument : IPoeQueryArgument
    {
        int Value { get; }
    }
}