namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryStringArgument : IPoeQueryArgument
    {
        string Value { get; }
    }
}