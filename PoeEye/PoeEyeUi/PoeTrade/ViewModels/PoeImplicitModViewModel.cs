namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System.Linq;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    using PoeShared.Common;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Utilities;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeImplicitModViewModel : DisposableReactiveObject
    {
        private float? max;

        private float? min;

        private string selectedMod;

        private readonly ReactiveCommand<object> resetCommand; 

        public PoeImplicitModViewModel(
                [NotNull] IPoeQueryInfoProvider queryInfoProvider,
                [NotNull] IFactory<ISuggestionProvider, string[]> suggestionProviderFactory)
        {
            Guard.ArgumentNotNull(() => queryInfoProvider);

            KnownMods = queryInfoProvider
                .ModsList
                .Where(x => x.ModType == PoeModType.Implicit)
                .Select(x => x.Name)
                .ToArray();
            SuggestionProvider = suggestionProviderFactory.Create(KnownMods);

            resetCommand = ReactiveCommand.Create();
            resetCommand.Subscribe(Reset).AddTo(Anchors);
        }

        public string[] KnownMods { get; }

        public ISuggestionProvider SuggestionProvider { get; }

        public ICommand ResetCommand => resetCommand;

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

        public void Reset()
        {
            SelectedMod = null;
            Min = Max = null;
        }
    }
}