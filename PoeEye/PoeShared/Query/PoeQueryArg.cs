namespace PoeShared.Query
{
    public interface IPoeQueryArgument
    {
        string ArgumentName { get; }
    }

    public interface IPoeQueryStringArgument : IPoeQueryArgument
    {
        
    }
}