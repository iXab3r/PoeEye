using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.UI
{
    public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
    {
        private static readonly IFluentLog Log = typeof(TreeViewItemViewModel).PrepareLogger();

        private readonly SourceList<TreeViewItemViewModel> children = new();
 

        protected TreeViewItemViewModel()
        {
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
        }

        public ReadOnlyObservableCollection<ITreeViewItemViewModel> Children { get; } 

        public bool IsSelected { get; set; }

        public bool IsExpanded { get; set; } = true;

        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public ITreeViewItemViewModel Parent { get; set; }

        public IComparer<ITreeViewItemViewModel> SortComparer { get; set; }

        public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen { get; set; }

        public void Clear()
        {
            Children.ForEach(x => x.Parent = null);
        }
    }
}