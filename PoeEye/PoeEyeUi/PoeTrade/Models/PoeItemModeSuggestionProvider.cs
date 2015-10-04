namespace PoeEyeUi.PoeTrade.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using DuoVia.FuzzyStrings;

    using Guards;

    using PoeShared.Common;

    using WpfControls;

    internal sealed class PoeItemModeSuggestionProvider : ISuggestionProvider
    {
        private readonly IPoeItemMod[] knownMods;

        private readonly IDictionary<IPoeItemMod, string[]> wordsDictionary = new Dictionary<IPoeItemMod, string[]>();

        public PoeItemModeSuggestionProvider(IPoeItemMod[] knownMods)
        {
            Guard.ArgumentNotNull(() => knownMods);
            
            this.knownMods = knownMods;

            foreach (var poeItemMod in knownMods)
            {
                var wordsFromName = poeItemMod.Name.Split(new [] {" ", ", "}, StringSplitOptions.RemoveEmptyEntries);
                var wordsFromCodeName = poeItemMod.CodeName.Split(new [] {" ", ","}, StringSplitOptions.RemoveEmptyEntries);

                var allWords = wordsFromName.Concat(wordsFromCodeName).Distinct().ToArray();
                wordsDictionary[poeItemMod] = allWords;
            }
        }

        public IEnumerable GetSuggestions(string filter)
        {
            var filteredMods = knownMods
                .Where(x => IsMatch(filter, x))
                .ToArray();
            return filteredMods;
        }

        private bool IsMatch(string filter, IPoeItemMod mod)
        {
            string[] words;
            if (!wordsDictionary.TryGetValue(mod, out words))
            {
                return false;
            }
            return words.Any(x => x.FuzzyMatch(filter) >= 0.33);
        }
    }
}