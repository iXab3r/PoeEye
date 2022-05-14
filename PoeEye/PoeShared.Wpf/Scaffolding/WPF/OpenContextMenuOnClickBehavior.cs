using System.Windows.Input;

namespace PoeShared.Scaffolding.WPF;

public sealed class OpenContextMenuOnClickBehavior : OpenContextMenuBehavior
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObjectOnPreviewMouseLeftButtonDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObjectOnPreviewMouseLeftButtonDown;
    }

    private void AssociatedObjectOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        OpenContextMenu();
        e.Handled = true;
    }
}