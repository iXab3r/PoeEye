using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.ValueBoxes;

namespace PoeShared.UI;

public class StretchingTreeViewItem : TreeViewItem
{
    public StretchingTreeViewItem()
    {
        this.Loaded += StretchingTreeViewItem_Loaded;
    }

    private void StretchingTreeViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (this.VisualChildrenCount > 0)
        {
            Grid grid = this.GetVisualChild(0) as Grid;
            if (grid != null && grid.ColumnDefinitions.Count == 3)
            {
                grid.ColumnDefinitions.RemoveAt(2);
                grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            }
        }
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new StretchingTreeViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is StretchingTreeViewItem;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.Handled || !IsEnabled)
        {
            return;
        }

        if (e.ClickCount % 2 == 0)
        {
            SetCurrentValue(IsExpandedProperty, BooleanBoxes.Box(!IsExpanded));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (e.Handled || !IsEnabled)
        {
            return;
        }

        if (Focus())
        {
            e.Handled = true;
        }
    }
}