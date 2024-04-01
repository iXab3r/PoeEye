using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeTitle<TItem> : BlazorReactiveComponent
{
    private const double OffSetx = 25;

    private double dragTargetClientX = 0;

    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    private bool Draggable => TreeComponent.Draggable && !SelfNode.Disabled;

    private bool IsSwitcherOpen => SelfNode.Expanded && !SelfNode.IsLeaf;

    private bool IsSwitcherClose => !SelfNode.Expanded && !SelfNode.IsLeaf;

    protected ClassMapper TitleClassMapper { get; } = new();

    private void SetTitleClassMapper()
    {
        TitleClassMapper
            .Add("ant-tree-node-content-wrapper")
            .If("draggable", () => Draggable)
            .If("ant-tree-node-content-wrapper-open", () => IsSwitcherOpen)
            .If("ant-tree-node-content-wrapper-close", () => IsSwitcherClose)
            .If("ant-tree-node-selected", () => SelfNode.Selected);
    }

    protected override void OnInitialized()
    {
        SetTitleClassMapper();
        base.OnInitialized();
    }

    private async Task OnClick(MouseEventArgs args)
    {
        if (SelfNode.Selected)
        {
            return;
        }
        
        SelfNode.SetSelected(true);
        /*if (TreeComponent.OnClick.HasDelegate && args.Button == 0)
        {
            await TreeComponent.OnClick.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode, args));
        }
        */
        TreeComponent.UpdateBindData();
    }

    private async Task OnDblClick(MouseEventArgs args)
    {
        if (TreeComponent.OnDblClick.HasDelegate && args.Button == 0)
        {
            await TreeComponent.OnDblClick.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode, args));
        }
    }

    private async Task OnContextMenu(MouseEventArgs args)
    {
        if (TreeComponent.OnContextMenu.HasDelegate)
        {
            await TreeComponent.OnContextMenu.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode, args));
        }
    }

    private void OnDragStart(DragEventArgs e)
    {
        TreeComponent.DragItem = SelfNode;
        SelfNode.Expand(false);
        
        /*if (TreeComponent.OnDragStart.HasDelegate)
        {
            TreeComponent.OnDragStart.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode));
        }*/
    }

    private void OnDragLeave(DragEventArgs e)
    {
        SelfNode.DragTarget = false;
        SelfNode.SetParentTargetContainer();
        /*if (TreeComponent.OnDragLeave.HasDelegate)
        {
            TreeComponent.OnDragLeave.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode));
        }*/
    }

    private void OnDragEnter(DragEventArgs e)
    {
        if (TreeComponent.DragItem == SelfNode)
        {
            return;
        }

        SelfNode.DragTarget = true;
        dragTargetClientX = e.ClientX;

     /*   if (TreeComponent.OnDragEnter.HasDelegate)
        {
            TreeComponent.OnDragEnter.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode));
        }*/
    }

    /// <summary>
    ///     Can be treated as a child if the target is moved to the right beyond the OffsetX distance
    /// </summary>
    /// <param name="e"></param>
    private void OnDragOver(DragEventArgs e)
    {
        if (TreeComponent.DragItem == SelfNode)
        {
            return;
        }

        if (e.ClientX - dragTargetClientX > OffSetx)
        {
            SelfNode.SetTargetBottom();
            SelfNode.SetParentTargetContainer();
            SelfNode.Expand(true);
        }
        else
        {
            SelfNode.SetTargetBottom(true);
            SelfNode.SetParentTargetContainer(true);
        }
    }

    private void OnDrop(DragEventArgs e)
    {
        SelfNode.DragTarget = false;
        SelfNode.SetParentTargetContainer();
        if (SelfNode.IsTargetBottom)
        {
            TreeComponent.DragItem.DragMoveDown(SelfNode);
        }
        else
        {
            TreeComponent.DragItem.DragMoveInto(SelfNode);
        }

        if (TreeComponent.OnDrop.HasDelegate)
        {
            TreeComponent.OnDrop.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, TreeComponent.DragItem, e, SelfNode.IsTargetBottom) {TargetNode = SelfNode});
        }
    }

    private void OnDragEnd(DragEventArgs e)
    {
        if (TreeComponent.OnDragEnd.HasDelegate)
        {
            TreeComponent.OnDragEnd.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, TreeComponent.DragItem) {TargetNode = SelfNode});
        }
    }
}