namespace PoeEyeUi.PoeTrade.Models
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


        public FuzzySuggestionProvider([NotNull] string[] haystack)
        {
            Guard.ArgumentNotNull(() => haystack);

            searchService = new LcsSearchService(haystack);
        }

        public IEnumerable GetSuggestions(string filter)
        {
            var filteredStrings = searchService
                .Search(filter)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Result)
                .Take(MaxResults)
                .ToArray();
            return filteredStrings;
        }
    }
}