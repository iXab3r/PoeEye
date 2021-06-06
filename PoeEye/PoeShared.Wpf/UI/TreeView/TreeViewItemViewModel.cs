using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI
{
    public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TreeViewItemViewModel));

        private readonly SourceList<TreeViewItemViewModel> children = new();

        private bool isExpanded = true;
        private bool isSelected;
        private ITreeViewItemViewModel parent;
        private IComparer<ITreeViewItemViewModel> sortComparer;
        private Func<ITreeViewItemViewModel, IObservable<Unit>> resortWhen;

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

        public bool IsSelected
        {
            get => isSelected;
            set => RaiseAndSetIfChanged(ref isSelected, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => RaiseAndSetIfChanged(ref isExpanded, value);
        }

        public ITreeViewItemViewModel Parent
        {
            get => parent;
            set => RaiseAndSetIfChanged(ref parent, value);
        }

        public IComparer<ITreeViewItemViewModel> SortComparer 
        {
            get => sortComparer;
            set => RaiseAndSetIfChanged(ref sortComparer, value);
        }

        public Func<ITreeViewItemViewModel, IObservable<Unit>> ResortWhen
        {
            get => resortWhen;
            set => RaiseAndSetIfChanged(ref resortWhen, value);
        }

        public void Clear()
        {
            Children.ForEach(x => x.Parent = null);
        }
    }
}