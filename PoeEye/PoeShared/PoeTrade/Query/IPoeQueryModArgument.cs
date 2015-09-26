namespace PoeShared.PoeTrade.Query
{
    public interface IPoeQueryModArgument : IPoeQueryArgument
    {
        bool Excluded { get; }
    }
}