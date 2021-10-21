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

        private readonly SourceList<TreeViewItemViewModel> children = new();
        private readonly ObservableAsPropertyHelper<string> pathSupplier;
        protected readonly Fallback<string> TabName = new();

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
                    Log.Debug($"[{this}] Changing Directory Path {x.prev} => {x.curr}");
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
                .Transform(x => (ITreeViewItemViewModel)x)
                .Sort(this.WhenAnyValue(x => x.SortComparer), SortOptions.None, resort)
                .Bind(out var chld)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Children = chld;

            Disposable.Create(() =>
            {
                this.Parent = null;
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
            
            Binder.Attach(this).AddTo(Anchors);
        }

        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true;

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
    }
}