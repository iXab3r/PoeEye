namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryRangeModArgument : IPoeQueryModArgument
    {
        float? Min { get; }

        float? Max { get; }
    }
}