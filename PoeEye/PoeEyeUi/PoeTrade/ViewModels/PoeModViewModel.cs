namespace PoeEyeUi.PoeTrade.ViewModels
{
    using PoeShared.Utilities;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeModViewModel : DisposableReactiveObject
    {
        private float? max;

        private float? min;

        private string selectedMod;

        public PoeModViewModel(ISuggestionProvider suggestionProvider)
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
    }
}