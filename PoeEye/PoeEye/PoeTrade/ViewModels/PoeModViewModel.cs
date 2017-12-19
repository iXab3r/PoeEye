namespace PoeEye.PoeTrade.ViewModels
{
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using WpfAutoCompleteControls.Editors;

    internal sealed class PoeModViewModel : DisposableReactiveObject, IPoeModViewModel
    {
        private float? max;

        private float? min;

        private string selectedMod;
        private ISuggestionProvider suggestionProvider;

        public PoeModViewModel()
        {
            this.WhenAnyValue(x => x.SelectedMod, x => x.Min, x => x.Max)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsEmpty)))
                .AddTo(Anchors);
        }

        public ISuggestionProvider SuggestionProvider
        {
            get { return suggestionProvider; }
            set { this.RaiseAndSetIfChanged(ref suggestionProvider, value); }
        }

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

        public bool IsEmpty => string.IsNullOrWhiteSpace(SelectedMod) && Min == null && Max == null;

        public void Reset()
        {
            SelectedMod = null;
            Min = null;
            Max = null;
        }
    }
}