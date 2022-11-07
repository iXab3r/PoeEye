using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

public class BringSelectedItemIntoViewBehaviorForSelector : Behavior<Selector>
{
    private static readonly IFluentLog Log = typeof(BringSelectedItemIntoViewBehavior).PrepareLogger();

    private readonly SerialDisposable attachmentAnchor = new SerialDisposable();

    protected override void OnAttached()
    {
        base.OnAttached();

        var anchors = new CompositeDisposable();
        attachmentAnchor.Disposable = anchors;

        AssociatedObject
            .Observe(Selector.SelectedItemProperty)
            .Select(_ => AssociatedObject.SelectedItem)
            .SubscribeSafe(selectedItem =>
            {
                var frameworkElement = selectedItem switch
                {
                    FrameworkElement fe => fe,
                    _ => AssociatedObject.ItemContainerGenerator.ContainerFromItem(selectedItem) as FrameworkElement,
                };

                if (frameworkElement == null)
                {
                    return;
                }
                Log.Debug(() => $"Bringing item into view: {selectedItem}");
                frameworkElement.BringIntoView();
            }, Log.HandleUiException)
            .AddTo(anchors);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        attachmentAnchor.Disposable = null;
    }
}