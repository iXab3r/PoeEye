namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Utilities;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeExplicitModViewModel : DisposableReactiveObject
    {
        private bool excluded;
        private float? max;

        private float? min;

        private string selectedMod;

        public PoeExplicitModViewModel(ISuggestionProvider suggestionProvider)
        {
            SuggestionProvider = suggestionProvider;
        }

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

        public bool Excluded
        {
            get { return excluded; }
            set { this.RaiseAndSetIfChanged(ref excluded, value); }
        }
    }
}