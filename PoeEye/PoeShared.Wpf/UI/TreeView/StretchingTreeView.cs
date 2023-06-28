using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public class StretchingTreeView : System.Windows.Controls.TreeView
{
    private ScrollViewer scrollViewer;
    
    public StretchingTreeView()
    {
        RequestBringIntoView += OnRequestBringIntoView;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        
        this.scrollViewer = this.FindVisualChildren<ScrollViewer>().FirstOrDefault();
    }

    protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
    {
        //this is an attempt to fix the issue with "jumping" viewport whenever some complex item gets selected

        if (scrollViewer == null)
        {
            return;
        }
        
        var horizontalOffset = scrollViewer.HorizontalOffset;
        var verticalOffset = scrollViewer.VerticalOffset;

        UpdateLayout();

        scrollViewer.ScrollToHorizontalOffset(horizontalOffset);
        scrollViewer.ScrollToVerticalOffset(verticalOffset);
        
        base.OnSelectedItemChanged(e);
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new StretchingTreeViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is StretchingTreeViewItem;
    }        
    
    private static void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
        e.Handled = true;
    }
}