using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.UI
{
    public interface ITreeViewItemViewModel : IDisposableReactiveObject
    {
        public string Name { get; set; }
     
        string Path { get; }
        
        public bool IsExpanded { get; set; }

        public bool IsSelected { get; set; }
        
        public bool IsVisible { get; }

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