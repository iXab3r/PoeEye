using ReactiveUI;

namespace PoeEye.StashGrid.ViewModels {
    internal class HighlightedStashGridCellViewModel : BasicStashGridCellViewModel
    {
        private bool isFresh;

        public bool IsFresh
        {
            get { return isFresh; }
            set { this.RaiseAndSetIfChanged(ref isFresh, value); }
        }

        private string toolTipText;

        public string ToolTipText
        {
            get { return toolTipText; }
            set { this.RaiseAndSetIfChanged(ref toolTipText, value); }
        }
    }
}