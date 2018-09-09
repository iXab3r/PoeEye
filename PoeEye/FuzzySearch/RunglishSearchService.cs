using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace FuzzySearch
{
    public sealed class RunglishSearchService : IFuzzySearchService
    {
        private static readonly IDictionary<char, char> russianToEnglishMap = new Dictionary<char, char>
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

        private readonly IFuzzySearchService searchService;

        public RunglishSearchService([NotNull] IFuzzySearchService searchService)
        {
            if (searchService == null)
            {
                throw new ArgumentNullException(nameof(searchService));
            }

            this.searchService = searchService;
        }

        public IEnumerable<SearchResult> Search(string needle)
        {
            var result = searchService
                         .Search(needle)
                         .ToArray();

            if (result.Length == 0)
            {
                // direct search failed, trying 'converted' search
                var converted = ConvertToEnglishLayout(needle);
                if (!string.Equals(converted, needle))
                {
                    return Search(converted);
                }
            }

            return result;
        }

        private static string ConvertToEnglishLayout(string source)
        {
            var result = source
                         .Select(
                             x =>
                             {
                                 char converted;
                                 if (russianToEnglishMap.TryGetValue(x, out converted))
                                 {
                                     return converted;
                                 }

                                 return x;
                             }).ToArray();
            return new string(result);
        }
    }
}