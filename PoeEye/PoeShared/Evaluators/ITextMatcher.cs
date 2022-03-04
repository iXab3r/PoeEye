namespace PoeShared.Evaluators;

public interface ITextMatcher
{
    bool IsMatch(string needle, string haystack);
}