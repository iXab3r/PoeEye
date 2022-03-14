using System.Text.RegularExpressions;
using Splat;

namespace PoeShared.Evaluators;

public sealed class RegexTextMatcher : ITextMatcher
{
    private static readonly IFluentLog Log = typeof(RegexTextMatcher).PrepareLogger();

    private readonly MemoizingMRUCache<RegexKey, Regex> regexCache = new(
        (key, _) =>
        {
            try
            {
                return new Regex(key.Pattern, key.Options | RegexOptions.Compiled);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to convert pattern to regex: {key}", e);
                return null;
            }
        }, 64
    );

    public bool IsMatch(string needle, string haystack, bool matchCase)
    {
        try
        {
            var regex = regexCache.Get(new RegexKey(needle, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase));
            return regex?.IsMatch(haystack) ?? false;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to match: {new { needle, haystack, matchCase }}", e);
            return false;
        }
    }

    private sealed record RegexKey
    {
        public RegexKey(string pattern, RegexOptions options)
        {
            this.Pattern = pattern;
            this.Options = options;
        }

        public string Pattern { get; }
        public RegexOptions Options { get; }
    }
}