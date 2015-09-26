namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryRangeModArgument : IPoeQueryModArgument
    {
        int Min { get; }

        int Max { get; }
    }
}