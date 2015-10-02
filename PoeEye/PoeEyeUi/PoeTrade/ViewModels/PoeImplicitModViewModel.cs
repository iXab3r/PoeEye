namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System.Linq;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    using WpfControls;

    internal sealed class PoeImplicitModViewModel : ReactiveObject
    {
        private float? max;

        private float? min;

        private IPoeItemMod selectedMod;

        public PoeImplicitModViewModel(
                [NotNull] IPoeQueryInfoProvider queryInfoProvider,
                [NotNull] IFactory<PoeItemModeSuggestionProvider, IPoeItemMod[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);

            KnownMods = queryInfoProvider.ModsList.Where(x => x.ModType == PoeModType.Implicit).ToArray();
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
    }
}