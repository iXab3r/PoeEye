namespace PoeEyeUi.PoeTrade.ViewModels
{
    using Guards;

    using PoeShared.Common;

    using ReactiveUI;

    internal sealed class PoeExplicitModViewModel : ReactiveObject
    {
        private float? max;

        private float? min;

        private IPoeItemMod selectedMod;

        public PoeExplicitModViewModel(IPoeItemMod[] explicitMods)
        {
            Guard.ArgumentNotNull(() => explicitMods);

            KnownMods = explicitMods;
        }

        public IPoeItemMod[] KnownMods { get; }

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