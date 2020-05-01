using System.Windows;

namespace PoeShared.UI.TreeView
{
    public class StretchingTreeView : System.Windows.Controls.TreeView
    {
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new StretchingTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is StretchingTreeViewItem;
        }        
    }
}