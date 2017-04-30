using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels {
    internal class HighlightedStashGridCellViewModel : BasicStashGridCellViewModel
    {
        private bool isFresh;

        public bool IsFresh
        {
            get { return isFresh; }
            set { this.RaiseAndSetIfChanged(ref isFresh, value); }
        }
    }
}