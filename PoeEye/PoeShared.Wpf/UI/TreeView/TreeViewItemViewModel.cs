using System.Collections.ObjectModel;
using PoeShared.Scaffolding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using log4net;

namespace PoeShared.UI.TreeView
{
    public abstract class TreeViewItemViewModel : DisposableReactiveObject, ITreeViewItemViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TreeViewItemViewModel));

        private bool isExpanded = true;
        private static readonly Predicate<ITreeViewItemViewModel> TRUE = model => true;

        private bool isSelected;
        private ITreeViewItemViewModel parent;

        private readonly SourceList<TreeViewItemViewModel> children =
            new SourceList<TreeViewItemViewModel>();

        public TreeViewItemViewModel(TreeViewItemViewModel parent)
        {
            children
                .Connect()
                .Transform(x => (ITreeViewItemViewModel)x)
                .Bind(out var chld)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            Children = chld;
            Parent = parent;
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

        public IEnumerable<ITreeViewItemViewModel> FindChildren(Predicate<ITreeViewItemViewModel> predicate)
        {
            return this.FindChildren(predicate, Children);
        }
        
        public IEnumerable<ITreeViewItemViewModel> FindParents()
        {
            return this.FindParents(TRUE, this);
        }

        public void Clear()
        {
            Children.ForEach(x => x.Parent = null);
        }
    }
}