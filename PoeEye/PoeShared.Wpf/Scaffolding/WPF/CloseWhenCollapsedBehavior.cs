using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CloseWhenCollapsedBehavior : Behavior<Window>
    {
        private readonly SerialDisposable anchors = new SerialDisposable();
        
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Observe(UIElement.VisibilityProperty)
                .Skip(1)
                .Subscribe(HandleVisibilityChange)
                .AssignTo(anchors);
        }

        private void HandleVisibilityChange()
        {
            if (AssociatedObject.Visibility == Visibility.Collapsed)
            {
                AssociatedObject.Close();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            anchors.Disposable = null;
        }
    }
}