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
        
        public AnnotatedBoolean IsVisible { get; }
        
        public bool IsEnabled { get; }
        
        public bool ParentIsExpanded { get; }

        public ITreeViewItemViewModel Parent { [CanBeNull] get; [CanBeNull] set; }
        
        public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }
        
        public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }

        void Clear();

        /// <summary>
        ///  Thread-safe enumeration, although it may contain partially modified data
        /// </summary>
        /// <returns></returns>
        IEnumerable<ITreeViewItemViewModel> EnumerateChildren();
    }
    
    public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
    {
    }
}