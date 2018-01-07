using System.Windows.Media;
using PoeEye.StashGrid.Models;
using ReactiveUI;

namespace PoeEye.StashGrid.ViewModels
{
    internal class HighlightedStashGridCellViewModel : BasicStashGridCellViewModel
    {
        private Color? borderColor;
        private bool isFresh;
        private string toolTipText;

        public bool IsFresh
        {
            get => isFresh;
            set => this.RaiseAndSetIfChanged(ref isFresh, value);
        }

        public string ToolTipText
        {
            get => toolTipText;
            set => this.RaiseAndSetIfChanged(ref toolTipText, value);
        }

        public Color? BorderColor
        {
            get => borderColor;
            set => this.RaiseAndSetIfChanged(ref borderColor, value);
        }
    }
}