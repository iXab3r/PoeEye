using System.Windows;

namespace PoeShared.UI;

public class StretchingTreeView : System.Windows.Controls.TreeView
{
    public StretchingTreeView()
    {
        RequestBringIntoView += OnRequestBringIntoView;
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