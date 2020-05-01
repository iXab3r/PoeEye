using System.Collections.ObjectModel;
using PoeShared.Scaffolding;

namespace PoeShared.UI.TreeView
{
    public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
    {
        private bool isExpanded;

        private bool isSelected;

        public TreeViewItemViewModel(ITreeViewItemViewModel parent)
        {
            Parent = parent;
        }

        public ObservableCollection<ITreeViewItemViewModel> Children { get; } =
            new ObservableCollection<ITreeViewItemViewModel>();

        public bool IsSelected
        {
            get => isSelected;
            set => RaiseAndSetIfChanged(ref isSelected, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => RaiseAndSetIfChanged(ref isExpanded, value);
        }

        public ITreeViewItemViewModel Parent { get; }
    }
}