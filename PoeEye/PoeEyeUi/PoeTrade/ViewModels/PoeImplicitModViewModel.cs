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

        private string selectedMod;

        public PoeImplicitModViewModel(
                [NotNull] IPoeQueryInfoProvider queryInfoProvider,
                [NotNull] IFactory<GenericSuggestionProvider, string[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);

            KnownMods = queryInfoProvider
                .ModsList
                .Where(x => x.ModType == PoeModType.Implicit)
                .Select(x => x.Name)
                .ToArray();
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
    }
}