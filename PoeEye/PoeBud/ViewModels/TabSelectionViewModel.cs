using Guards;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class TabSelectionViewModel : ReactiveObject
    {
        public TabSelectionViewModel(string tabName)
        {
            Guard.ArgumentNotNull(tabName, nameof(tabName));

            Name = tabName;
        }

        public string Name { get; }

        private bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set { this.RaiseAndSetIfChanged(ref isSelected, value); }
        }
    }
}
