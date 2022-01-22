using GongSolutions.Wpf.DragDrop;

namespace PoeShared.UI;

internal sealed class HotkeyDragHandler : DefaultDragHandler
{
    public override bool CanStartDrag(IDragInfo dragInfo)
    {
        if (dragInfo.Data == System.Windows.Data.CollectionView.NewItemPlaceholder || 
            dragInfo.SourceItem == System.Windows.Data.CollectionView.NewItemPlaceholder)
        {
            return false;
        }
        return base.CanStartDrag(dragInfo);
    }
}