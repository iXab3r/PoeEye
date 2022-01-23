using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface ITreeViewItemViewModel : IDisposableReactiveObject
{
    string Name { get; }
    
    /// <summary>
    ///   Full path, i.e. item name and folder name
    /// </summary>
    string Path { get; }
        
    public bool IsExpanded { get; set; }

    public bool IsSelected { get; set; }
        
    public AnnotatedBoolean IsVisible { get; }
        
    public bool IsEnabled { get; }
        
    public bool ParentIsExpanded { get; }

    public ITreeViewItemViewModel Parent { [CanBeNull] get; }
        
    public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }
        
    public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

    /// <summary>
    ///   Not thread-safe for enumeration
    /// </summary>
    public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { [NotNull] get; }
    
    /// <summary>
    ///  Supports thread-safe enumeration, although it may contain partially modified data
    /// </summary>
    /// <returns></returns>
    public IObservableList<ITreeViewItemViewModel> ChildrenList { [NotNull] get; }
}
    
public interface IDirectoryTreeViewItemViewModel : ITreeViewItemViewModel
{
}