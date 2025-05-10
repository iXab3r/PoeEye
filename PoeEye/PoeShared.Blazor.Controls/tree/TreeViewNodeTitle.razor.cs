using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AntDesign;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeTitle<TItem> : BlazorReactiveComponent
{
    public TreeViewNodeTitle()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsDraggable));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsDroppable));
    }

    [CascadingParameter(Name = "Tree")]
    public required TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public required TreeViewNode<TItem> SelfNode { get; set; }

    private ClassMapper TitleClassMapper { get; } = new();

    protected override void OnInitialized()
    {
        SetTitleClassMapper();
        base.OnInitialized();

        var trackers = new ReactiveTrackerList()
        {
            this.WhenAnyValue(x => x.SelfNode.Selected),
            this.WhenAnyValue(x => x.SelfNode.IsSwitcherOpen),
            this.WhenAnyValue(x => x.SelfNode.IsSwitcherClose)
        };
        trackers.Merge().Subscribe(x => Class = TitleClassMapper.ToString()).AddTo(Anchors);
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
    }

    protected override void OnAfterFirstRender()
    {
        base.OnAfterFirstRender();
        
        this.WhenAnyValue(x => x.Class)
            .Skip(1)
            .Subscribe(x =>
            {
                try
                {
                    TreeComponent.JsPoeBlazorUtils.SetClass(ElementRef, x);
                }
                catch (Exception)
                {
                    if (Anchors.IsDisposed)
                    {
                        return;
                    }

                    throw;
                }
            })
            .AddTo(Anchors);
    }

    private void SetTitleClassMapper()
    {
        TitleClassMapper
            .Add("ant-tree-node-content-wrapper")
            .If("draggable", () => SelfNode.IsDraggable)
            .If("ant-tree-node-content-wrapper-open", () => SelfNode.IsSwitcherOpen)
            .If("ant-tree-node-content-wrapper-close", () => SelfNode.IsSwitcherClose)
            .If("ant-tree-node-selected", () => SelfNode.Selected);
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
                if (TreeComponent.IsShiftKeyDown && 
                    selectedNodes.Any() && 
                    false) //does not work due to list reshuffling, has to be rewritten
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
                    if (!selectedNodes.Add(SelfNode))
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
            await TreeComponent.OnClick.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
            {
                Node = SelfNode,
                OriginalEvent = args
            });
        }
    }

    private async Task OnDblClick(MouseEventArgs args)
    {
        if (TreeComponent.OnDblClick.HasDelegate && args.Button == 0)
        {
            await TreeComponent.OnDblClick.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
            {
                Node = SelfNode,
                OriginalEvent = args
            });
        }
    }

    private async Task OnContextMenu(MouseEventArgs args)
    {
        if (TreeComponent.OnContextMenu.HasDelegate)
        {
            await TreeComponent.OnContextMenu.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
            {
                Node = SelfNode,
                OriginalEvent = args
            });
        }
    }

    private Task OnDragStart(DragEventArgs e)
    {
        return SelfNode.OnDragStart(e);
    }
    
    private Task OnDragEnd(DragEventArgs e)
    {
        return SelfNode.OnDragEnd(e);
    }

    private Task OnDragLeave(DragEventArgs e)
    {
        return SelfNode.OnDragLeave(e);
    }

    private Task OnDragEnter(DragEventArgs e)
    {
        return SelfNode.OnDragEnter(e);
    }

    private Task OnDragOver(DragEventArgs e)
    {
        return SelfNode.OnDragOver(e);
    }

    private Task OnDrop(DragEventArgs e)
    {
        return SelfNode.OnDrop(e);
    }
}