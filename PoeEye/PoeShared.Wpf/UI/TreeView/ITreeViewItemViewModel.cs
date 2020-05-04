using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.UI.TreeView
{
    public interface ITreeViewItemViewModel : IDisposableReactiveObject
    {
        bool IsExpanded { get; set; }
        
        bool IsSelected { get; set; }
        
        ITreeViewItemViewModel Parent { [CanBeNull] get; [CanBeNull] set; }

        ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }
    }
    
    public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
    {
    }
}