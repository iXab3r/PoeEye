using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeTitle<TItem> : BlazorReactiveComponent
{
    private static readonly Binder<TreeViewNodeTitle<TItem>> Binder = new();

    private const double OffSetx = 25;

    private double dragTargetClientX = 0;
    
    static TreeViewNodeTitle()
    {
        Binder.BindIf(x => x.TreeComponent != null && x.SelfNode != null, x => x.TreeComponent.Draggable && !x.SelfNode.Disabled && x.SelfNode.Draggable)
            .To(x => x.Draggable);
        
        Binder.BindIf(x => x.TreeComponent != null && x.SelfNode != null, x => x.TreeComponent.Draggable && !x.SelfNode.Disabled && x.SelfNode.Droppable)
            .To(x => x.Droppable);
    }

    public TreeViewNodeTitle()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.Draggable));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.Droppable));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.Selected));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsSwitcherOpen));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsSwitcherOpen));
        
        Binder.Attach(this).AddTo(Anchors);
    }

    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    protected ClassMapper TitleClassMapper { get; } = new();
    
    public bool Draggable { get; private set; }
    
    public bool Droppable { get; private set; }
    
    [Inject]
    public IJsPoeBlazorUtils JsPoeBlazorUtils { get; init; }

    private void SetTitleClassMapper()
    {
        TitleClassMapper
            .Add("ant-tree-node-content-wrapper")
            .If("draggable", () => Draggable)
            .If("ant-tree-node-content-wrapper-open", () => SelfNode.IsSwitcherOpen)
            .If("ant-tree-node-content-wrapper-close", () => SelfNode.IsSwitcherClose)
            .If("ant-tree-node-selected", () => SelfNode.Selected);
    }

    protected override void OnInitialized()
    {
        SetTitleClassMapper();
        base.OnInitialized();
    }

    private async Task OnClick(MouseEventArgs args)
    {

        switch (TreeComponent.SelectionMode)
        {
            case TreeViewSelectionMode.Disabled:
            {
                await TreeComponent.SetSelection(new HashSet<TreeViewNode<TItem>>());
                break;
            }
            case TreeViewSelectionMode.SingleItem:
            {
                await TreeComponent.SetSelection(new HashSet<TreeViewNode<TItem>>(){ SelfNode });
                break;
            }
            case TreeViewSelectionMode.MultipleItems:
            {
                var selectedNodes = TreeComponent
                    .NodesById
                    .Items
                    .Where(x => x.Selected)
                    .ToHashSet();
                if (TreeComponent.IsShiftKeyDown && selectedNodes.Any())
                {
                    var allNodes = TreeComponent.NodesById
                        .Items
                        .OrderBy(x => x.TreeLevel)
                        .ThenBy(x => x.NodeIndex)
                        .ToList();

                    // Find the index of the currently clicked node and the first selected node in the list
                    var clickedNodeIndex = allNodes.IndexOf(SelfNode);
                    var firstSelectedNodeIndex = allNodes.IndexOf(selectedNodes.First());

                    // Determine the range of nodes to select based on the positions of the clicked and the first selected nodes
                    var rangeStart = Math.Min(clickedNodeIndex, firstSelectedNodeIndex);
                    var rangeEnd = Math.Max(clickedNodeIndex, firstSelectedNodeIndex);

                    var rangeSelection = new HashSet<TreeViewNode<TItem>>();
                    for (var i = rangeStart; i <= rangeEnd; i++)
                    {
                        rangeSelection.Add(allNodes[i]);
                    }

                    if (TreeComponent.IsCtrlKeyDown)
                    {
                        rangeSelection.UnionWith(selectedNodes);
                    }

                    await TreeComponent.SetSelection(rangeSelection);
                }
                else if (TreeComponent.IsCtrlKeyDown)
                {
                    if (selectedNodes.Contains(SelfNode))
                    {
                        selectedNodes.Add(SelfNode);
                    }
                    else
                    {
                        selectedNodes.Remove(SelfNode);
                    }
                    await TreeComponent.SetSelection(selectedNodes);
                }
                else
                {
                    await TreeComponent.SetSelection(new HashSet<TreeViewNode<TItem>>(){ SelfNode });
                }
                
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (TreeComponent.OnClick.HasDelegate && args.Button == 0)
        {
            await TreeComponent.OnClick.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, SelfNode, args));
        }
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

    private async Task OnDragStart(DragEventArgs e)
    {
        TreeComponent.DragItem = SelfNode;
        await SelfNode.Expand(false);
    }

    private void OnDragLeave(DragEventArgs e)
    {
        SelfNode.SetDragTarget(false);
        SelfNode.SetParentTargetContainer(false);
    }

    private void OnDragEnter(DragEventArgs e)
    {
        if (TreeComponent.DragItem == SelfNode)
        {
            return;
        }

        SelfNode.SetDragTarget(true);
        dragTargetClientX = e.ClientX;
    }

    /// <summary>
    ///     Can be treated as a child if the target is moved to the right beyond the OffsetX distance
    /// </summary>
    /// <param name="e"></param>
    private async Task OnDragOver(DragEventArgs e)
    {
        if (TreeComponent.DragItem == SelfNode)
        {
            return;
        }

        if (e.ClientX - dragTargetClientX > OffSetx)
        {
            if (SelfNode.Expanded == false && SelfNode.IsLeaf == false)
            {
                await SelfNode.Expand(true);
            }
            
            SelfNode.SetTargetBottom(false);
            SelfNode.SetParentTargetContainer(false);
        }
        else if (SelfNode.Droppable)
        {
            SelfNode.SetTargetBottom(true);
            SelfNode.SetParentTargetContainer(true);
        }
    }

    private async Task OnDrop(DragEventArgs e)
    {
        SelfNode.SetDragTarget(false);
        SelfNode.SetParentTargetContainer(false);

        var dragItem = TreeComponent.DragItem;
        if (dragItem != null)
        {
            if (SelfNode.IsTargetBottom)
            {
                await dragItem.DragMoveDown(SelfNode);
            }
            else
            {
                await dragItem.DragMoveInto(SelfNode);
            }
        }

        if (TreeComponent.OnDrop.HasDelegate)
        {
            await TreeComponent.OnDrop.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent, TreeComponent.DragItem, e, SelfNode.IsTargetBottom) {TargetNode = SelfNode});
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