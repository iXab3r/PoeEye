namespace PoeShared.Evaluators;

public sealed class RunglishTextMatcher : ITextMatcher
{
    private static readonly IDictionary<char, char> RussianToEnglishMap = new Dictionary<char, char>
    {
        {'й', 'q'},
        {'ц', 'w'},
        {'у', 'e'},
        {'к', 'r'},
        {'е', 't'},
        {'н', 'y'},
        {'г', 'u'},
        {'ш', 'i'},
        {'щ', 'o'},
        {'з', 'p'},
        {'ф', 'a'},
        {'ы', 's'},
        {'в', 'd'},
        {'а', 'f'},
        {'п', 'g'},
        {'р', 'h'},
        {'о', 'j'},
        {'л', 'k'},
        {'д', 'l'},
        {'я', 'z'},
        {'ч', 'x'},
        {'с', 'c'},
        {'м', 'v'},
        {'и', 'b'},
        {'т', 'n'},
        {'ь', 'm'}
    };

    private readonly ITextMatcher matcher;

    public RunglishTextMatcher(ITextMatcher matcher)
    {
        this.matcher = matcher;
    }

    public bool IsMatch(string needle, string haystack)
    {
        var result = matcher.IsMatch(needle, haystack);
        if (result)
        {
            return true;
        }

        // direct search failed, trying 'converted' search
        var converted = ConvertToEnglishLayout(needle);
        return matcher.IsMatch(converted, haystack);
    }

    private static string ConvertToEnglishLayout(string source)
    {
        var result = source
            .Select(
                x =>
                {
                    char converted;
                    if (RussianToEnglishMap.TryGetValue(x, out converted))
                    {
                        return converted;
                    }

                    return x;
                }).ToArray();
        return new string(result);
    }
}