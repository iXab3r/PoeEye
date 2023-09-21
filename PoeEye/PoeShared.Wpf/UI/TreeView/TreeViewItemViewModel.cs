using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.UI;

public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
{
    private static readonly Binder<TreeViewItemViewModel> Binder = new();
    private static readonly IFluentLog Log = typeof(TreeViewItemViewModel).PrepareLogger();
    private static readonly IComparer<ITreeViewItemViewModel> DefaultComparer = new SortExpressionComparer<ITreeViewItemViewModel>();

    private readonly SourceListEx<ITreeViewItemViewModel> children = new();
    protected readonly Fallback<string> TabName = new();
    private readonly ObservableAsPropertyHelper<bool> parentIsExpanded;

    static TreeViewItemViewModel()
    {
        Binder.Bind(x => System.IO.Path.GetFileName(x.FullPath ?? string.Empty)).To(x => x.Name);
        Binder.Bind(x => System.IO.Path.GetDirectoryName(x.FullPath ?? string.Empty)).To(x => x.FolderName);
        Binder.Bind(x => System.IO.Path.Combine(x.FolderName ?? string.Empty, x.Name ?? string.Empty)).To(x => x.FullPath);
    }

    protected TreeViewItemViewModel()
    {
        ChildrenList = children.AsObservableList();
            
        this.WhenAnyValue(x => x.Parent.ResortWhen)
            .SubscribeSafe(x => ResortWhen = x, Log.HandleUiException)
            .AddTo(Anchors);
        
        this.WhenAnyValue(x => x.Parent.RefreshWhen)
            .SubscribeSafe(x => RefreshWhen = x, Log.HandleUiException)
            .AddTo(Anchors);
            
        this.WhenAnyValue(x => x.Parent.SortComparer)
            .SubscribeSafe(x => SortComparer = x, Log.HandleUiException)
            .AddTo(Anchors);

        var resort = this.WhenAnyValue(x => x.ResortWhen)
            .Select(x => x != null ? x(this) : Observable.Return(Unit.Default))
            .Switch();
        var refresh = this.WhenAnyValue(x => x.RefreshWhen)
            .Select(x => x != null ? x(this) : Observable.Never(Unit.Default))
            .Select(x => x)
            .Switch();
        children
            .Connect()
            .AutoRefreshOnObservableSynchronized(model => refresh)
            .Sort(this.WhenAnyValue(x => x.SortComparer).Select(x => x ?? DefaultComparer), SortOptions.None, resort) // DynamicData 7+ REQUIRES to have Comparer set to non-null
            .BindToCollection(out var chld)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        Children = chld;

        Disposable.Create(() =>
        {
            Parent = null;
        }).AddTo(Anchors);
            
        this.WhenAnyValue(x => x.Parent)
            .Cast<TreeViewItemViewModel>()
            .WithPrevious((prev, curr) => new { prev, curr })
            .SubscribeSafe(x =>
            {
                if (x.prev == x.curr)
                {
                    return;
                }
                    
                x.prev?.children.Remove(this);
                x.curr?.children.Add(this);
            }, Log.HandleUiException)
            .AddTo(Anchors);
            
        parentIsExpanded = this.WhenAnyValue(x => x.Parent)
            .Select(x => x is IDirectoryTreeViewItemViewModel eyeItem 
                ? eyeItem.WhenAnyValue(y => y.ParentIsExpanded).CombineLatest(eyeItem.WhenAnyValue(y => y.IsExpanded), (parentIsExpanded, isExpanded) => parentIsExpanded && isExpanded) 
                : Observable.Return(true))
            .Switch()
            .ToProperty(this, x => x.ParentIsExpanded)
            .AddTo(Anchors);
            
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsEnabled { get; set; } = true;

    public AnnotatedBoolean IsVisible { get; set; } = new(true, "Visible by default");
        
    public AnnotatedBoolean IsExpandable { get; set; } = new(true, "Expandable by default");
        
    public bool ParentIsExpanded => parentIsExpanded.Value;

    public bool MatchesFilter { get; } = true;

    public Func<ITreeViewItemViewModel, IObservable<Unit>> RefreshWhen { get; set; }
    
    public IReadOnlyObservableCollection<ITreeViewItemViewModel> Children { get; }
    public IObservableList<ITreeViewItemViewModel> ChildrenList { get; }
    public Func<ITreeViewItemViewModel, IObservable<bool>> Filter { get; set; }

    public bool IsSelected { get; set; }

    public string FullPath { get; set; }
    
    public string FolderName { get; set; }

    public bool IsExpanded { get; set; } = true;

    public ITreeViewItemViewModel Parent { get; set; }

    public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }

    public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

    public string Name
    {
        get => TabName.Value;
        set => TabName.SetValue(value);
    }

    private static IEnumerable<ITreeViewItemViewModel> EnumerateChildren(TreeViewItemViewModel root)
    {
        if (root.children.Count <= 0)
        {
            yield break;
        }
            
        foreach (var child in root.children.Items)
        {
            yield return child;

            if (child is not TreeViewItemViewModel treeChild)
            {
                throw new InvalidOperationException($"Enumeration is supported only for items of type {typeof(TreeViewItemViewModel)}, but got child {child} with type {child.GetType()}");
            }

            foreach (var childOfChild in EnumerateChildren(treeChild))
            {
                yield return childOfChild;
            }
        }
    }
}