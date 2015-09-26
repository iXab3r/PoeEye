namespace PoeShared.PoeTrade
{
    public interface IPoeQueryArgument
    {
        string ArgumentName { get; }
    }

    public interface IPoeQueryStringArgument : IPoeQueryArgument
    {
        
    }

    public interface IPoeQueryModArgument
    {
        
    }
}