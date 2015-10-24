namespace FuzzySearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DuoVia.FuzzyStrings;

    using JetBrains.Annotations;

    public sealed class LcsSearchService : IFuzzySearchService
    {
        private readonly string[] haystack;

        public LcsSearchService([NotNull] string[] haystack)
        {
            if (haystack == null)
            {
                throw new ArgumentNullException(nameof(haystack));
            }
            this.haystack = haystack;
        }

        public IEnumerable<SearchResult> Search(string needle)
        {
            if (needle == null)
            {
                return new SearchResult[0];
            }

            return haystack
                .Select(itemToMatch => new { itemToMatch, Score = LongestCommonSubsequenceExtensions.LongestCommonSubsequence(itemToMatch, needle).Item2 }) 
                .Where(x => x.Score > 0)
                .Select(x => new SearchResult(x.itemToMatch, x.Score * 100));
        }
    }
}