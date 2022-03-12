namespace PoeShared.Evaluators;

public sealed class StrictTextMatcher : ITextMatcher
{
    public StrictTextMatcher()
    {
    }

    public bool IsMatch(string needle, string haystack, bool matchCase)
    {
        return haystack.Contains(needle, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }
}