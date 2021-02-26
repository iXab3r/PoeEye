using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.UI.TreeView
{
    public interface ITreeViewItemViewModel : IDisposableReactiveObject
    {
        public bool IsExpanded { get; set; }

        public bool IsSelected { get; set; }

        public ITreeViewItemViewModel Parent { [CanBeNull] get; [CanBeNull] set; }

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }

        void Clear();
    }
    
    public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
    {
    }
}