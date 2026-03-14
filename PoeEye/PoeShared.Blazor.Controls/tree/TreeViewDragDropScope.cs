using System.Collections.Generic;

namespace PoeShared.Blazor.Controls;

public sealed class TreeViewDragDropScope<TItem>
{
    internal IDictionary<(long, long), TreeViewDragDropInfo> DragDropStateByNodeIds { get; } = new Dictionary<(long, long), TreeViewDragDropInfo>();

    internal TreeViewNode<TItem>? DragDropNode { get; set; }

    internal TreeViewNode<TItem>? DragDropTargetContainerNode { get; set; }

    internal TreeViewNode<TItem>? DragDropTargetBelowNode { get; set; }

    internal TreeViewNode<TItem>? DragDropTargetNode { get; set; }

    internal void Clear()
    {
        DragDropStateByNodeIds.Clear();
        DragDropNode = null;
        DragDropTargetNode = null;
        DragDropTargetBelowNode = null;
        DragDropTargetContainerNode = null;
    }
}
