using System;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls;

public class TreeViewEventArgs<TItem> : EventArgs
{
    public TreeViewEventArgs()
    {
    }

    public TreeViewEventArgs(TreeView<TItem> tree)
    {
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));
    }

    public TreeView<TItem>? Tree { get; init; }
    
    public TreeViewNode<TItem>? Node { get; init; }

    public TreeViewNode<TItem>? TargetNode { get; init; }
    
    public MouseEventArgs OriginalEvent { get; init; }

    public bool DropBelow { get; init; }
}