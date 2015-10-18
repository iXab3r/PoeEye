﻿namespace PoeEyeUi.PoeTrade.Models
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Guards;

    using JetBrains.Annotations;

    using WpfControls.Editors;

    internal sealed class GenericSuggestionProvider : ISuggestionProvider
    {
        private const double MinScore = 15.0;

        private readonly IEnumerable<string> haystack;
        private readonly FuzzySearchService searchService;

        public GenericSuggestionProvider([NotNull] string[] haystack)
        {
            Guard.ArgumentNotNull(() => haystack);

            searchService = new FuzzySearchService(haystack);
        }

        public IEnumerable GetSuggestions(string filter)
        {
            var filteredStrings = searchService
                .Search(filter)
                .Where(x => x.Score > MinScore)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Result)
                .ToArray();
            return filteredStrings;
        }
    }
}