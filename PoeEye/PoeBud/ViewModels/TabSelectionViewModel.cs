using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.ViewModels
{
    using Guards;
    using ReactiveUI;

    internal sealed class TabSelectionViewModel : ReactiveObject
    {
        public TabSelectionViewModel(ITab tab)
        {
            Guard.ArgumentNotNull(() => tab);
            
            this.Tab = tab;
        }

        public string Name => $"[{Tab.Idx}] {Tab.Name}";

        private bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set { this.RaiseAndSetIfChanged(ref isSelected, value); }
        }

        public ITab Tab { get; }
    }
}