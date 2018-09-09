using Guards;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal sealed class TabSelectionViewModel : ReactiveObject
    {
        private bool isSelected;

        public TabSelectionViewModel(string tabName)
        {
            Guard.ArgumentNotNull(tabName, nameof(tabName));

            Name = tabName;
        }

        public string Name { get; }

        public bool IsSelected
        {
            get => isSelected;
            set => this.RaiseAndSetIfChanged(ref isSelected, value);
        }
    }
}