using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

public abstract class OpenContextMenuBehavior : Behavior<FrameworkElement>
{
    protected void OpenContextMenu()
    {
        if (AssociatedObject?.ContextMenu == null)
        {
            return;
        }
        if (AssociatedObject.ContextMenu.IsOpen)
        {
            return;
        }

        AssociatedObject.ContextMenu.Placement = PlacementMode.Bottom;
        AssociatedObject.ContextMenu.PlacementTarget = AssociatedObject;
        AssociatedObject.ContextMenu.IsOpen = true;
    }
}