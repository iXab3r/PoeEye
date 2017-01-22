using System;

namespace PoeEye.PoeTrade.Models
{
    using System.Collections;
    using System.Linq;

    using FuzzySearch;

    using Guards;

    using JetBrains.Annotations;

    using WpfAutoCompleteControls.Editors;

    internal sealed class FuzzySuggestionProvider : ISuggestionProvider
    {
        private const int MaxResults = 20;
        private readonly IFuzzySearchService searchService;

        public FuzzySuggestionProvider(
            [NotNull] string[] haystack)
        {
            Guard.ArgumentNotNull(() => haystack);

            searchService = new XSearchService<string>(haystack, x => x);
        }

        public IEnumerable GetSuggestions(string filter)
        {
            var filteredStrings = searchService
                .Search(filter)
                .Select(x => x.Result)
                .Take(MaxResults)
                .ToArray();
            return filteredStrings;
        }
    }
}