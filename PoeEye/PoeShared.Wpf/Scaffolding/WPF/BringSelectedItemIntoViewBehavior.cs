using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Interactivity;
using Microsoft.VisualBasic.Logging;
using PoeShared.Logging;
using PoeShared.UI;

namespace PoeShared.Scaffolding.WPF
{
    public class BringSelectedItemIntoViewBehavior : Behavior<TreeView>
    {
        private static readonly IFluentLog Log = typeof(BringSelectedItemIntoViewBehavior).PrepareLogger();

        private readonly SerialDisposable attachmentAnchor = new SerialDisposable();

        protected override void OnAttached()
        {
            base.OnAttached();

            var anchors = new CompositeDisposable();
            attachmentAnchor.Disposable = anchors;

            AssociatedObject
                .Observe(TreeView.SelectedItemProperty)
                .Select(_ => AssociatedObject.SelectedItem)
                .OfType<TreeViewItem>()
                .SubscribeSafe(treeViewItem =>
                {
                    if (treeViewItem == null)
                    {
                        return;
                    }
                    Log.Debug(() => $"Bringing item into view: {treeViewItem}");
                    treeViewItem.BringIntoView();
                }, Log.HandleUiException)
                .AddTo(anchors);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            attachmentAnchor.Disposable = null;
        }
    }
}