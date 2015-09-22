using JetBrains.Annotations;

namespace PoeShared
{
    public interface IPoeSearchResult
    {
        string Raw { [CanBeNull] get; } 
    }
}