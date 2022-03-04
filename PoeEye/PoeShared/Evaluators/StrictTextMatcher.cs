namespace PoeShared.Evaluators;

public sealed class StrictTextMatcher : ITextMatcher
{
    private readonly StringComparison stringComparison;

    public StrictTextMatcher(StringComparison stringComparison)
    {
        this.stringComparison = stringComparison;
    }

    public bool IsMatch(string needle, string haystack)
    {
        return haystack.Contains(needle, stringComparison);
    }
}