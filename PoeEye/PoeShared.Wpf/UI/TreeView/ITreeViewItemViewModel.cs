using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.UI.TreeView
{
    public interface ITreeViewItemViewModel : IDisposableReactiveObject
    {
        public bool IsExpanded { get; set; }

        public bool IsSelected { get; set; }

        public ITreeViewItemViewModel Parent { [CanBeNull] get; [CanBeNull] set; }
        
        public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }
        
        public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }

        void Clear();
    }
    
    public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
    {
        public bool ParentIsExpanded { get; }
    }
}