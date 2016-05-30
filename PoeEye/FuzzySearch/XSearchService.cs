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

            this.haystack = haystack;
        }

        public IEnumerable<SearchResult> Search(string needle)
        {
            if (string.IsNullOrWhiteSpace(needle))
            {
                return new SearchResult[0];
            }

            try
            {
                needle = PreprocessRegex(needle);
                var words = needle.Split(new[] { "(", ")", "#", " ", ",", "." }, StringSplitOptions.RemoveEmptyEntries);
                var regexText = string.Join(".*?", words);

                var regexOptions = RegexOptions.Compiled;
                if (!caseSensitive)
                {
                    regexOptions |= RegexOptions.IgnoreCase;
                }
                var regex = new Regex(regexText, regexOptions);

                return haystack
                    .Select(x => Search(x, regex))
                    .Where(x => x.Score > 0)
                    .OrderBy(x => x.Score)
                    .ThenBy(x => x.Result.Length)
                    .ToArray();
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new SearchResult($"Internal exception: {ex.Message}", 0), 
                };
            }

            
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

        private string PreprocessRegex(string regex)
        {
            regex = regex.Replace(".*", ".*?");
            regex = regex.Replace("*", ".*?");
            return regex;
        }
    }
}