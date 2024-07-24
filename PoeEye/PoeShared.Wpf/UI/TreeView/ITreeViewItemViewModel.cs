using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Common;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface ITreeViewItemViewModel : IDisposableReactiveObject, ICanBeSelected
{
    string Name { get; }
    
    /// <summary>
    ///   Full path, i.e. item name and folder name
    /// </summary>
    string FullPath { get; }
        
    bool IsExpanded { get; set; }

    AnnotatedBoolean IsVisible { get; }
        
    bool IsEnabled { get; }
        
    bool ParentIsExpanded { get; }

    bool MatchesFilter { get; }
    
    ITreeViewItemViewModel Parent { [CanBeNull] get; }
        
    IComparer<ITreeViewItemViewModel> SortComparer { get; set; }
        
    IReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }
    
    /// <summary>
    ///  Supports thread-safe enumeration, although it may contain partially modified data
    /// </summary>
    /// <returns></returns>
    IObservableList<ITreeViewItemViewModel> ChildrenList { [NotNull] get; }
    
    Func<ITreeViewItemViewModel, IObservable<bool>> Filter { get; set; }
}
    
public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
{
}