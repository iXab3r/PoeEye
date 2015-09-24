namespace PoeShared
{
    public interface IPoeCurrency
    {
        string CodeName { get; }
        
        string Name { get; }
        
        string Uri { get; } 
    }
}