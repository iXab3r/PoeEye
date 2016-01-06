namespace FuzzySearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    public class XSearchService : IFuzzySearchService
    {
        private readonly bool caseSensitive;
        private readonly string[] haystack;

        public XSearchService([NotNull] string[] haystack, bool caseSensitive = false)
        {
            this.caseSensitive = caseSensitive;
            if (haystack == null)
            {
                throw new ArgumentNullException(nameof(haystack));
            }

            this.haystack = caseSensitive ? haystack : haystack.Select(x => x.ToLower()).ToArray();
        }

        public IEnumerable<SearchResult> Search(string needle)
        {
            if (string.IsNullOrWhiteSpace(needle))
            {
                return new SearchResult[0];
            }

            needle = caseSensitive ? needle : needle.ToLower();
            var words = needle.Split(new[] { "(", ")", "#", " ", ",", "." }, StringSplitOptions.RemoveEmptyEntries);
            var regexText = string.Join(".*?", words);
            var regex = new Regex(regexText, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            return haystack
                .Select(x => Search(x, regex))
                .Where(x => x.Score > 0)
                .OrderBy(x => x.Score)
                .ThenBy(x => x.Result.Length)
                .ToArray();
        }

        private SearchResult Search(string candidate, Regex regex)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return new SearchResult(string.Empty, 0.0);
            }

            var match = regex.Match(candidate);

            return new SearchResult(candidate, match.Success ? match.Length : 0);
        }
    }
}