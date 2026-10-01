using System.Collections.Generic;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

/// <summary>
/// Shared reactive drag state for one or more tree views. Node binders observe this
/// scope directly so receiving indicators, drop destination and clearing stay in sync.
/// </summary>
public sealed class TreeViewDragDropScope<TItem> : ReactiveObject
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
