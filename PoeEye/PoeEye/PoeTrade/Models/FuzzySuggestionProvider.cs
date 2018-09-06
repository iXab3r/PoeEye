using System.Collections.Generic;

namespace PoeEye.PoeTrade.Models
{
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
            [NotNull] IEnumerable<string> haystack)
        {
            Guard.ArgumentNotNull(haystack, nameof(haystack));

            searchService = new RunglishSearchService(new XSearchService<string>(haystack, x => x));
        }

        public System.Collections.IEnumerable GetSuggestions(string filter)
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
