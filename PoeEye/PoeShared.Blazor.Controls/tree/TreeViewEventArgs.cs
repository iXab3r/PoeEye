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

    public TreeViewEventArgs(TreeView<TItem> tree, TreeViewNode<TItem> node)
    {
        Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        Node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public TreeViewEventArgs(TreeView<TItem> tree, TreeViewNode<TItem>? node, MouseEventArgs originalEvent)
    {
        Tree = tree;
        Node = node;
        OriginalEvent = originalEvent;
    }

    public TreeViewEventArgs(TreeView<TItem> tree, TreeViewNode<TItem>? node, MouseEventArgs originalEvent, bool dropBelow)
    {
        Tree = tree;
        Node = node;
        OriginalEvent = originalEvent;
        DropBelow = dropBelow;
    }

    public TreeView<TItem>? Tree { get; set; }
    
    public TreeViewNode<TItem>? Node { get; set; }

    public TreeViewNode<TItem>? TargetNode { get; set; }

    public MouseEventArgs? OriginalEvent { get; set; }

    public bool DropBelow { get; set; }
}