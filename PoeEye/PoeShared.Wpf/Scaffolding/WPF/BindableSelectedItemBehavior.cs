using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using log4net;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF
{
    public class BindableSelectedItemBehavior : Behavior<TreeView>
    {
        private static readonly IFluentLog Log = typeof(BindableSelectedItemBehavior).PrepareLogger();

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(BindableSelectedItemBehavior),
                new UIPropertyMetadata(null, OnSelectedItemChanged));

        
        private readonly SerialDisposable attachmentAnchor = new SerialDisposable();

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
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
            if (e.NewValue is TreeViewItem item)
            {
                item.SetValue(TreeViewItem.IsSelectedProperty, true);
            }
        }

        private void OnTreeViewSelectedItemChanged(object previousValue, object currentValue)
        {
            Log.Debug($"[{AssociatedObject}({AssociatedObject.Name})] Changing {SelectedItem} => {currentValue}");
            SelectedItem = currentValue;
            Log.Debug($"[{AssociatedObject}({AssociatedObject.Name})] Selected item changed {previousValue} => {SelectedItem}");
        }
    }
}