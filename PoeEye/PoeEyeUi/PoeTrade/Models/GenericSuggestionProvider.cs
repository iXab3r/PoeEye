namespace PoeEyeUi.PoeTrade.Models
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
        private readonly IDictionary<string, string[]> wordsDictionary = new Dictionary<string, string[]>();

        public GenericSuggestionProvider([NotNull] string[] haystack)
        {
            Guard.ArgumentNotNull(() => haystack);
            
            this.haystack = haystack;
            foreach (var item in haystack)
            {
                var wordsFromName = item.Split(new[] { " ", ", " }, StringSplitOptions.RemoveEmptyEntries);

                var allWords = wordsFromName.Distinct().ToArray();
                wordsDictionary[item] = allWords;
            }
        }

        public IEnumerable GetSuggestions(string filter)
        {
            var filteredMods = haystack
                .Where(x => IsMatch(filter, x))
                .ToArray();
            return filteredMods;
        }

        private bool IsMatch(string filter, string item)
        {
            string[] words;
            if (!wordsDictionary.TryGetValue(item, out words))
            {
                return false;
            }
            return words.Any(x => x.FuzzyMatch(filter) >= 0.33);
        }
    }
}