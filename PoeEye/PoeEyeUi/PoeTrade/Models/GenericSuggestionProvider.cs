﻿namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using DuoVia.FuzzyStrings;

    using Guards;

    using JetBrains.Annotations;

    using WpfControls;

    internal sealed class GenericSuggestionProvider : ISuggestionProvider
    {
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
                .Where(x => x.Score > 15)
                .Select(x => x.Result)
                .ToArray();
            return filteredStrings;
        }
    }
}