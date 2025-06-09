using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AntDesign;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNode<TItem> : BlazorReactiveComponent
{
    private static readonly Binder<TreeViewNode<TItem>> Binder = new();

    private readonly ClassMapper classMapper = new();
    private const double MinDragDistanceOffsetX = 25;

    private double dragTargetClientX;
    private string icon;
    private string key;
    private string title;

    static TreeViewNode()
    {
        Binder.Bind(x => x.Expanded && !x.IsLeaf).To(x => x.IsSwitcherOpen);
        Binder.Bind(x => !x.Expanded && !x.IsLeaf).To(x => x.IsSwitcherClose);
        Binder.Bind(x => ReferenceEquals(x.TreeComponent.DragDropTargetContainerNode, x)).To(x => x.IsTargetContainer);
        Binder.Bind(x => ReferenceEquals(x.TreeComponent.DragDropTargetBelowNode, x)).To(x => x.IsTargetBelow);
        Binder.Bind(x => ReferenceEquals(x.TreeComponent.DragDropTargetNode, x)).To(x => x.IsDragTarget);

        Binder.Bind(x => x.TreeComponent.Draggable && !x.Disabled && x.Draggable).To(x => x.IsDraggable);
        Binder.Bind(x => x.TreeComponent.Draggable && !x.Disabled && x.Droppable).To(x => x.IsDroppable);
        
        Binder.Bind(x => !x.Hidden && (x.ParentNode == null || (x.ParentNode.Expanded && x.ParentNode.IsVisible))).To(x => x.IsVisible);
        Binder.Bind(x => x.Disabled || (x.ParentNode != null && x.ParentNode.Disabled)).To(x => x.IsDisabled);
    }

    public TreeViewNode()
    {
        NodeId = TreeViewHelper.GetNextNodeId();
    }

    [CascadingParameter(Name = "Tree")] public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "Node")] public TreeViewNode<TItem>? ParentNode { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string Key
    {
        get
        {
            if (TreeComponent.KeyExpression != null)
            {
                return TreeComponent.KeyExpression(this);
            }

            return key;
        }
        set => key = value;
    }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter] public bool Selected { get; set; }

    [Parameter] public EventCallback<bool> SelectedChanged { get; set; }

    [Parameter] public bool Expanded { get; set; }

    [Parameter] public bool Draggable { get; set; }

    [Parameter] public bool Droppable { get; set; } = true;

    [Parameter]
    public string Icon
    {
        get => TreeComponent.IconExpression != null ? TreeComponent.IconExpression(this) : icon;
        set => icon = value;
    }

    [Parameter] public RenderFragment<TreeViewNode<TItem>> IconTemplate { get; set; }

    [Parameter]
    public string Title
    {
        get
        {
            if (TreeComponent.TitleExpression != null)
            {
                return TreeComponent.TitleExpression(this);
            }

            return title;
        }
        set => title = value;
    }

    [Parameter] public RenderFragment? TitleTemplate { get; set; }

    [Parameter] public TItem DataItem { get; set; }

    [Parameter] public bool Hidden { get; set; }

    public bool IsLeaf { get; private set; } = true;

    public int TreeLevel => (ParentNode?.TreeLevel ?? -1) + 1;

    public bool IsSwitcherOpen { get; [UsedImplicitly] private set; }

    public bool IsSwitcherClose { get; [UsedImplicitly] private set; }

    public bool IsDragTarget { get; [UsedImplicitly] private set; }

    public bool IsTargetBelow { get; [UsedImplicitly] private set; }

    public bool IsTargetContainer { get; [UsedImplicitly] private set; }

    public bool IsLastNode => NodeIndex == (ParentNode?.ChildNodes.Count ?? TreeComponent?.ChildNodes.Count) - 1;

    internal bool IsDroppable { get; [UsedImplicitly] private set; }
    internal bool IsDraggable { get; [UsedImplicitly] private set; }
    internal int NodeIndex { get; set; }
    internal long NodeId { get; }
    internal bool IsVisible { get; [UsedImplicitly] private set; }
    internal bool IsDisabled { get; [UsedImplicitly] private set; }

    private List<TreeViewNode<TItem>> ChildNodes { get; } = new();
    
    private bool SwitcherOpen => Expanded && !IsLeaf;

    private bool SwitcherClose => !Expanded && !IsLeaf;

    private IList<TItem> ChildDataItems
    {
        get
        {
            if (TreeComponent.ChildrenExpression != null)
            {
                var childItems = TreeComponent.ChildrenExpression(this);
                if (childItems is IList<TItem> list)
                {
                    return list;
                }

                return childItems?.ToList() ?? new List<TItem>();
            }

            return new List<TItem>();
        }
    }

    public void SetExpanded(bool expanded)
    {
        if (Expanded == expanded)
        {
            return;
        }

        Expanded = expanded;
    }

    public async Task Expand(bool expanded)
    {
        SetExpanded(expanded);
        await TreeComponent.OnNodeExpand(this, Expanded, new MouseEventArgs());
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
    }

    public override string ToString()
    {
        var sb = new ToStringBuilder(this);
        sb.AppendParameter(nameof(Key), Key);
        if (Selected)
        {
            sb.AppendParameter(nameof(Selected), Selected);
        }

        if (Expanded)
        {
            sb.AppendParameter(nameof(Expanded), Expanded);
        }

        return sb.ToString();
    }

    public IEnumerable<TreeViewNode<TItem>> EnumerateChildrenAndSelf()
    {
        yield return this;

        foreach (var childNode in ChildNodes)
        {
            yield return childNode;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        TreeComponent.RemoveNode(this);
        await base.DisposeAsync();
    }

    internal async Task DragMoveInto(TreeViewNode<TItem> treeNode)
    {
        if (TreeComponent.DataSource == null || !TreeComponent.DataSource.Any())
        {
            return;
        }

        if (treeNode == this || DataItem.Equals(treeNode.DataItem))
        {
            return;
        }

        Remove();

        treeNode.AddChildNode(DataItem);
        treeNode.IsLeaf = false;
        await treeNode.Expand(true);
    }

    internal async Task DragMoveDown(TreeViewNode<TItem> treeNode)
    {
        if (TreeComponent.DataSource == null || !TreeComponent.DataSource.Any())
        {
            return;
        }

        if (treeNode == this || DataItem.Equals(treeNode.DataItem))
        {
            return;
        }

        Remove();
        await treeNode.AddNextNode(DataItem);
    }

    protected override void OnAfterFirstRender()
    {
        base.OnAfterFirstRender();

        this.WhenAnyValue(x => x.Class)
            .Skip(1)
            .SubscribeAsync(async x =>
            {
                try
                {
                    await TreeComponent.JsPoeBlazorUtils.SetClass(ElementRef, x);
                }
                catch (Exception ex)
                {
                    if (Anchors.IsDisposed)
                    {
                        return;
                    }

                    if (ex is JSException)
                    {
                        Log.Warn($"JS exception when tried to update the class to {x} in {this}", ex);
                        return;
                    }

                    throw;
                }
            })
            .AddTo(Anchors);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (ParentNode != null)
        {
            ParentNode.AddNode(this);
        }
        else
        {
            TreeComponent.AddChildNode(this);
        }

        TreeComponent.AddNode(this);

        if (TreeComponent.DisabledExpression != null)
        {
            Disabled = TreeComponent.DisabledExpression(this);
        }

        if (TreeComponent.ExpandedKeys != null)
        {
            await Expand(TreeComponent.ExpandedKeys.Any(k => k == Key));
        }

        SetTreeViewNodeClassMapper();

        Binder.Attach(this).AddTo(Anchors);
        
        var classTracker = new ReactiveTrackerList()
            .With(this.WhenAnyValue(x => x.IsVisible, x => x.IsDisabled, x => x.Selected))
            .With(this.WhenAnyValue(x => x.SwitcherClose, x => x.SwitcherOpen))
            .With(this.WhenAnyValue(x => x.IsTargetContainer, x => x.IsTargetBelow, x => x.IsDragTarget, x => x.IsLastNode));

        classTracker.Merge()
            .Subscribe(x => Class = classMapper.ToString())
            .AddTo(Anchors);
    }

    private IList<TItem> GetParentChildDataItems()
    {
        if (ParentNode != null)
        {
            return ParentNode.ChildDataItems;
        }

        return TreeComponent.DataSource as IList<TItem> ?? TreeComponent.DataSource.ToList();
    }

    private void AddChildNode(TItem dataItem)
    {
        ChildDataItems.Add(dataItem);
    }

    private async Task AddNextNode(TItem dataItem)
    {
        var parentChildDataItems = GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(DataItem);
        parentChildDataItems.Insert(index + 1, dataItem);

        await AddNodeAndSelect(dataItem);
    }

    private void Remove()
    {
        var parentChildDataItems = GetParentChildDataItems();
        parentChildDataItems.Remove(DataItem);
    }

    private void AddNode(TreeViewNode<TItem> treeNode)
    {
        treeNode.NodeIndex = ChildNodes.Count;
        ChildNodes.Add(treeNode);
        IsLeaf = false;
    }

    private void SetTreeViewNodeClassMapper()
    {
        classMapper
            .Add("ant-tree-treenode")
            .If("d-none", () => !IsVisible)
            .If("ant-tree-treenode-disabled", () => IsDisabled)
            .If("ant-tree-treenode-switcher-open", () => SwitcherOpen)
            .If("ant-tree-treenode-switcher-close", () => SwitcherClose)
            .If("ant-tree-treenode-selected", () => Selected)
            .If("drop-target", () => IsDragTarget)
            .If("drag-over-gap-bottom", () => IsDragTarget && IsTargetBelow)
            .If("drag-over", () => IsDragTarget && !IsTargetBelow)
            .If("drop-container", () => IsTargetContainer)
            .If("ant-tree-treenode-leaf-last", () => IsLastNode);
    }

    private async Task OnSwitcherClick(MouseEventArgs args)
    {
        Expanded = !Expanded;
        await TreeComponent.OnNodeExpand(this, Expanded, args);
    }

    private async Task AddNodeAndSelect(TItem dataItem)
    {
        var tn = ChildNodes.FirstOrDefault(treeNode => treeNode.DataItem.Equals(dataItem));
        if (tn != null)
        {
            await Expand(true);
            tn.Selected = true;
        }
    }

    internal async Task OnDragStart(DragEventArgs e)
    {
        TreeComponent.NotifyDragStart(this);
        await Expand(false);
        if (TreeComponent.OnDragStart.HasDelegate)
        {
            await TreeComponent.OnDragStart.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
            {
                Node = TreeComponent.DragDropNode,
                TargetNode = this
            });
        }
    }

    internal async Task OnDragEnd(DragEventArgs e)
    {
        if (TreeComponent.OnDragEnd.HasDelegate)
        {
            await TreeComponent.OnDragEnd.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
            {
                Node = TreeComponent.DragDropNode,
                TargetNode = this
            });
        }

        TreeComponent.NotifyDragEnd();
    }

    internal async Task OnDragLeave(DragEventArgs e)
    {
        //drag-leave in HTML is not really reliable as any spacing could trigger that event
        TreeComponent.SetDragDropTargetBelowNode(null);
        TreeComponent.SetDragDropTargetContainerNode(null);
    }

    internal async Task OnDragEnter(DragEventArgs e)
    {
        var dragItem = TreeComponent.DragDropNode;
        var state = new TreeViewDragDropInfo(CanDropBelow: CanDropBelow(dragItem, this), CanDropInto: CanDropInto(dragItem, this));
        TreeComponent.DragDropStateByNodeIds[(this.NodeId, dragItem?.NodeId ?? 0)] = state;

        if (state is { CanDropBelow: false, CanDropInto: false })
        {
            TreeComponent.SetDragTargetNode(null);
            dragTargetClientX = 0;
        }
        else
        {
            TreeComponent.SetDragTargetNode(this);
            dragTargetClientX = e.ClientX;
        }
    }

    internal async Task OnDragOver(DragEventArgs e)
    {
        var dragItem = TreeComponent.DragDropNode;
        var state = TreeComponent.DragDropStateByNodeIds[(this.NodeId, dragItem?.NodeId ?? 0)];

        if (e.ClientX - dragTargetClientX > MinDragDistanceOffsetX)
        {
            if (this is { Expanded: false, IsLeaf: false })
            {
                await this.Expand(true);
            }

            if (state.CanDropInto)
            {
                TreeComponent.SetDragDropTargetBelowNode(null);
                TreeComponent.SetDragDropTargetContainerNode(null);
                return;
            }
        }

        if (state.CanDropBelow && this.Droppable)
        {
            TreeComponent.SetDragDropTargetBelowNode(this);
            TreeComponent.SetDragDropTargetContainerNode(this.ParentNode);
        }
        else if (state.CanDropInto)
        {
            TreeComponent.SetDragDropTargetBelowNode(null);
            TreeComponent.SetDragDropTargetContainerNode(this.ParentNode);
        }
    }

    internal async Task OnDrop(DragEventArgs e)
    {
        try
        {
            var dragItem = TreeComponent.DragDropNode;
            if (dragItem != null)
            {
                if (this.IsTargetBelow)
                {
                    if (!CanDropBelow(TreeComponent.DragDropNode, this))
                    {
                        return;
                    }

                    await dragItem.DragMoveDown(this);
                }
                else
                {
                    if (!CanDropInto(TreeComponent.DragDropNode, this))
                    {
                        return;
                    }

                    await dragItem.DragMoveInto(this);
                }
            }

            if (TreeComponent.OnDrop.HasDelegate)
            {
                await TreeComponent.OnDrop.InvokeAsync(new TreeViewEventArgs<TItem>(TreeComponent)
                {
                    Node = TreeComponent.DragDropNode,
                    DropBelow = this.IsTargetBelow,
                    TargetNode = this
                });
            }
        }
        finally
        {
            TreeComponent.NotifyDragEnd();
        }
    }

    private bool CanDropBelow(TreeViewNode<TItem>? dragItem, TreeViewNode<TItem> targetItem)
    {
        if (!CanDrop(TreeComponent, dragItem, targetItem))
        {
            return false;
        }

        var canDropBelowExpression = TreeComponent.CanDropBelowExpression;
        if (canDropBelowExpression == null)
        {
            return true;
        }

        if (!canDropBelowExpression(dragItem, targetItem))
        {
            return false;
        }

        return true;
    }

    private bool CanDropInto(TreeViewNode<TItem>? dragItem, TreeViewNode<TItem> targetItem)
    {
        if (!CanDrop(TreeComponent, dragItem, targetItem))
        {
            return false;
        }

        var canDropInsideExpression = TreeComponent.CanDropInsideExpression;
        if (canDropInsideExpression == null)
        {
            return true;
        }

        if (!canDropInsideExpression(dragItem, targetItem))
        {
            return false;
        }

        return true;
    }

    private static bool CanDrop(TreeView<TItem> tree, TreeViewNode<TItem>? dragItem, TreeViewNode<TItem> targetItem)
    {
        if (dragItem == targetItem)
        {
            return false;
        }

        if (dragItem != null)
        {
            //dragItem could be null if we're dragging FROM OUTSIDE the tree
            var createsCircularReference = dragItem.EnumerateChildrenAndSelf().Any(x => ReferenceEquals(x, targetItem));
            if (createsCircularReference)
            {
                return false;
            }
        }

        return true;
    }
}