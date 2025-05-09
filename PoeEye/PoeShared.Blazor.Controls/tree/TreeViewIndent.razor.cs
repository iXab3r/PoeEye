// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewIndent<TItem> : BlazorReactiveComponent
{
    private static readonly Binder<TreeViewIndent<TItem>> Binder = new();

    static TreeViewIndent()
    {
        Binder.Bind(x => x.SelfNode.ParentNode).To(x => x.ParentNode);
        Binder.Bind(x => x.ParentNode != null &&  x.ParentNode.IsDraggable).To(x => x.IsDraggable);
        Binder.Bind(x => x.ParentNode != null &&  x.ParentNode.IsDroppable).To(x => x.IsDroppable);
    }

    public TreeViewIndent()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.ParentNode));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.IsDraggable));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.IsDroppable));
    }

    [CascadingParameter(Name = "Tree")]
    public required TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public required TreeViewNode<TItem> SelfNode { get; set; }
    
    public TreeViewNode<TItem>? ParentNode { get; [UsedImplicitly] private set; }

    [Parameter] public int TreeLevel { get; set; }

    public bool IsDraggable { get; [UsedImplicitly] private set; }
    
    public bool IsDroppable { get; [UsedImplicitly] private set; }
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Binder.Attach(this).AddTo(Anchors);
    }

    private static TreeViewNode<TItem> GetParentNode(TreeViewNode<TItem> node, int level)
    {
        if (level > 0 && node.ParentNode != null)
        {
            return GetParentNode(node.ParentNode, level - 1);
        }

        return node;
    }
    
    private Task OnDragStart(DragEventArgs e)
    {
        return ParentNode!.OnDragStart(e);
    }
    
    private Task OnDragEnd(DragEventArgs e)
    {
        return ParentNode!.OnDragEnd(e);
    }

    private Task OnDragLeave(DragEventArgs e)
    {
        return ParentNode!.OnDragLeave(e);
    }

    private Task OnDragEnter(DragEventArgs e)
    {
        return ParentNode!.OnDragEnter(e);
    }

    private Task OnDragOver(DragEventArgs e)
    {
        return ParentNode!.OnDragOver(e);
    }

    private Task OnDrop(DragEventArgs e)
    {
        return ParentNode!.OnDrop(e);
    }
}