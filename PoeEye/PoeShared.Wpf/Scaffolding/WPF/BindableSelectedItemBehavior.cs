using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using PoeShared.Common;
using PoeShared.Logging;
using PoeShared.UI;

namespace PoeShared.Scaffolding.WPF;

public class BindableSelectedItemBehavior : Behavior<TreeView>
{
    private static readonly IFluentLog Log = typeof(BindableSelectedItemBehavior).PrepareLogger();

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register("SelectedItem", typeof(ITreeViewItemViewModel), typeof(BindableSelectedItemBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        
    private readonly SerialDisposable attachmentAnchor = new SerialDisposable();

    public ITreeViewItemViewModel SelectedItem
    {
        get => (ITreeViewItemViewModel)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }
        
    protected override void OnAttached()
    {
        base.OnAttached();

        var anchors = new CompositeDisposable();
        attachmentAnchor.Disposable = anchors;
        AssociatedObject
            .Observe(TreeView.SelectedItemProperty)
            .Select(_ => AssociatedObject.SelectedItem)
            .Select(x =>
            {
                if (x != null && x is not ITreeViewItemViewModel)
                {
                    throw new ArgumentException($"{nameof(BindableSelectedItemBehavior)} supports only items of type {nameof(ITreeViewItemViewModel)}, got {x}");
                }
                return (ITreeViewItemViewModel)x;
            })
            .WithPrevious((prev, curr) => new { prev, curr })
            .SubscribeSafe(x => OnTreeViewSelectedItemChanged(x.prev, x.curr), Log.HandleUiException)
            .AddTo(anchors);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        attachmentAnchor.Disposable = null;
    }
        
    private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ICanBeSelected newItem)
        {
            newItem.IsSelected = true;
        }

        if (e.OldValue is ICanBeSelected oldItem)
        {
            oldItem.IsSelected = false;
        }
    }

    private void OnTreeViewSelectedItemChanged(ITreeViewItemViewModel previousValue, ITreeViewItemViewModel currentValue)
    {
        var previousSelectedItem = SelectedItem;
        Log.Debug($"[{AssociatedObject}({AssociatedObject.Name})] Changing {previousValue} => {currentValue}, SelectedItem: {previousSelectedItem}");
        SelectedItem = currentValue;
        Log.Debug($"[{AssociatedObject}({AssociatedObject.Name})] Selected item changed {previousSelectedItem} => {SelectedItem}");
    }
}