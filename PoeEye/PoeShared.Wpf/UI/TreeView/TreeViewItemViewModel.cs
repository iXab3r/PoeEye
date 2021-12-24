using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DynamicData;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.UI
{
    public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
    {
        private static readonly Binder<TreeViewItemViewModel> Binder = new();
        private static readonly IFluentLog Log = typeof(TreeViewItemViewModel).PrepareLogger();

        private readonly SourceList<ITreeViewItemViewModel> children = new();
        private readonly ObservableAsPropertyHelper<string> pathSupplier;
        protected readonly Fallback<string> TabName = new();
        private readonly ObservableAsPropertyHelper<bool> parentIsExpanded;

        static TreeViewItemViewModel()
        {
        }

        protected TreeViewItemViewModel()
        {
            pathSupplier = Observable.Merge(
                    this.WhenAnyValue(x => x.Parent)
                        .Select(x => x is IDirectoryTreeViewItemViewModel eyeItem ? eyeItem.WhenAnyValue(y => y.Path) : Observable.Return(string.Empty))
                        .Switch()
                        .Select(_ => "Parent directory path changed"),
                    this.WhenAnyValue(x => x.Name).Select(_ => "Directory name changed"))
                .Select(x => FindPath(this))
                .WithPrevious((prev, curr) => new {prev, curr})
                .Where(x => x.prev != x.curr)
                .DistinctUntilChanged()
                .Select(x =>
                {
                    if (string.IsNullOrEmpty(x.prev))
                    {
                        Log.Debug(() => $"[{this}] Setting Directory Path: {x.curr}");
                    }
                    else
                    {
                        Log.Debug(() => $"[{this}] Changing Directory Path {x.prev} => {x.curr}");
                    }
                    return x.curr;
                })
                .ToProperty(this, x => x.Path)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Parent.ResortWhen)
                .SubscribeSafe(x => ResortWhen = x, Log.HandleUiException)
                .AddTo(Anchors);
            
            this.WhenAnyValue(x => x.Parent.SortComparer)
                .SubscribeSafe(x => SortComparer = x, Log.HandleUiException)
                .AddTo(Anchors);

            var resort = this.WhenAnyValue(x => x.ResortWhen)
                .Select(x => x != null ? x(this) : Observable.Return(Unit.Default))
                .Switch();
            children
                .Connect()
                .Sort(this.WhenAnyValue(x => x.SortComparer), SortOptions.None, resort)
                .Bind(out var chld)
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

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { get; }

        public bool IsSelected { get; set; }

        public string Path => pathSupplier.Value;

        public bool IsExpanded { get; set; } = true;

        public ITreeViewItemViewModel Parent { get; set; }

        public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }

        public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

        public string Name
        {
            get => TabName.Value;
            set => TabName.SetValue(value);
        }

        public void Clear()
        {
            children.Items.ForEach(x => x.Parent = null);
        }

        public static ITreeViewItemViewModel FindRoot(ITreeViewItemViewModel node)
        {
            var result = node;
            while (result.Parent != null)
            {
                result = result.Parent;
            }
            return result;
        }

        public static string FindPath(ITreeViewItemViewModel node)
        {
            var resultBuilder = new StringBuilder();

            while (node != null)
            {
                if (node is IDirectoryTreeViewItemViewModel parentDir)
                {
                    resultBuilder.Insert(0, parentDir.Name + System.IO.Path.DirectorySeparatorChar);
                }

                node = node.Parent;
            }

            var result = resultBuilder.ToString().Trim(System.IO.Path.DirectorySeparatorChar);
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public IEnumerable<ITreeViewItemViewModel> EnumerateChildren()
        {
            return EnumerateChildren(this);
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
}