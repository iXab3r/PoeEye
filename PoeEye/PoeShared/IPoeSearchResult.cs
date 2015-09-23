namespace PoeShared
{
    using JetBrains.Annotations;

    public interface IPoeSearchResult
    {
        string Raw { [CanBeNull] get; }
    }
}