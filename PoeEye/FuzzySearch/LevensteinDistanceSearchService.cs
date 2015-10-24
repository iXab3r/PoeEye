namespace FuzzySearch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DuoVia.FuzzyStrings;

    using JetBrains.Annotations;

    public sealed class LevensteinDistanceSearchService : IFuzzySearchService
    {
        private readonly string[] haystack;

        public LevensteinDistanceSearchService([NotNull] string[] haystack)
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

            return haystack.Select(x => new SearchResult(x, 1f / x.LevenshteinDistance(needle)));
        }
    }
}