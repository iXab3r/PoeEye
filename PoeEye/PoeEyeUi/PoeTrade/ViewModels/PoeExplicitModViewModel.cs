namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

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

        private IPoeItemMod selectedMod;

        public PoeExplicitModViewModel(
                IPoeItemMod[] explicitMods,
                [NotNull] IFactory<PoeItemModeSuggestionProvider, IPoeItemMod[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => explicitMods);

            KnownMods = explicitMods;
            SuggestionProvider = suggestionProviderFactory.Create(KnownMods);
        }

        public IPoeItemMod[] KnownMods { get; }

        public ISuggestionProvider SuggestionProvider { get; }

        public IPoeItemMod SelectedMod
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