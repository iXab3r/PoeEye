namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;

    using ReactiveUI;

    using WpfControls;

    internal sealed class PoeExplicitModViewModel : ReactiveObject
    {
        private float? max;

        private float? min;

        private string selectedMod;

        public PoeExplicitModViewModel(
                IPoeItemMod[] explicitMods,
                [NotNull] IFactory<GenericSuggestionProvider, string[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => explicitMods);
            Guard.ArgumentIsTrue(() => explicitMods.All(x => x.ModType == PoeModType.Explicit));

            KnownMods = explicitMods.Select(x => x.Name).ToArray();
            SuggestionProvider = suggestionProviderFactory.Create(KnownMods);
        }

        public string[] KnownMods { get; }

        public ISuggestionProvider SuggestionProvider { get; }

        public string SelectedMod
        {
            get { return selectedMod; }
            set { this.RaiseAndSetIfChanged(ref selectedMod, value); }
        }

        public float? Min
        {
            get { return min; }
            set { this.RaiseAndSetIfChanged(ref min, value); }
        }

        public float? Max
        {
            get { return max; }
            set { this.RaiseAndSetIfChanged(ref max, value); }
        }

        private bool excluded;

        public bool Excluded
        {
            get { return excluded; }
            set { this.RaiseAndSetIfChanged(ref excluded, value); }
        }
    }
}