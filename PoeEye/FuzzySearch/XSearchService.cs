namespace FuzzySearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    public class XSearchService<T> : IFuzzySearchService
    {
        private readonly bool caseSensitive;
        private readonly T[] haystack;
        private readonly Func<T, string> mapper;

        public XSearchService(
            [NotNull] T[] haystack,
            [NotNull] Func<T, string> mapper, 
            bool caseSensitive = false)
        {
            if (haystack == null)
            {
                throw new ArgumentNullException(nameof(haystack));
            }
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            this.caseSensitive = caseSensitive;
            this.haystack = haystack;
            this.mapper = mapper;
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

        private SearchResult Search(T candidate, Regex regex)
        {
            var candidateText = mapper(candidate);
            if (string.IsNullOrWhiteSpace(candidateText))
            {
                return new SearchResult(string.Empty, 0.0);
            }

            var match = regex.Match(candidateText);

            return new SearchResult(candidateText, match.Success ? match.Length : 0, candidate);
        }

        private string PreprocessRegex(string regex)
        {
            regex = regex.Replace(".*", ".*?");
            regex = regex.Replace("*", ".*?");
            return regex;
        }
    }
}