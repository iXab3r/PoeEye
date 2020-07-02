using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System;

namespace PoeShared.Scaffolding.WPF
{
    public class BindableSelectedItemBehavior : Behavior<TreeView>
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(BindableSelectedItemBehavior),
                new UIPropertyMetadata(null));

        
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
                .Subscribe(x => OnTreeViewSelectedItemChanged(AssociatedObject.SelectedItem))
                .AddTo(anchors);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            attachmentAnchor.Disposable = null;
        }

        private void OnTreeViewSelectedItemChanged(object newValue)
        {
            SelectedItem = newValue;
        }
    }
}