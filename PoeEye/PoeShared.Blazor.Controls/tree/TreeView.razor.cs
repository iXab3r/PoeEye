using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.JsInterop;
using DynamicData;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeView<TItem> : BlazorReactiveComponent
{
    private readonly SourceCache<TreeViewNode<TItem>, long> nodesById = new(x => x.NodeId);

    private readonly ClassMapper classMapper = new();

    private TreeViewNode<TItem>? lastSelectedItem;

    public TreeView()
    {
        NodesByKey = nodesById
            .Connect()
            .ChangeKey(x => x.Key)
            .OnItemAdded(item =>
            {
                if (ShowExpandedByDefault != null)
                {
                    item.SetExpanded(ShowExpandedByDefault.Value);
                }
            })
            .OnItemRemoved(x =>
            {
                if (ReferenceEquals(lastSelectedItem, x))
                {
                    //reset selection of removed item
                    lastSelectedItem = null;
                }
            })
            .AsObservableCache()
            .AddTo(Anchors);

        SelectedItemsById = nodesById
            .Connect()
            .AutoRefreshOnObservable(x => x.WhenAnyValue(y => y.Selected))
            .Filter(x => x.Selected)
            .OnItemAdded(x =>
            {
                //remember last selected item as it may be of use for subsequent operations
                lastSelectedItem = x;
            })
            .AsObservableCache()
            .AddTo(Anchors);
    }

    [Parameter] public bool ShowExpand { get; set; } = true;

    [Parameter] public bool? ShowExpandedByDefault { get; set; }

    [Parameter] public bool ShowIcon { get; set; }

    [Parameter] public bool BlockNode { get; set; }

    [Parameter] public bool Draggable { get; set; }

    [Parameter] public bool Disabled { get; set; }
    
    [Parameter] public bool AutoExpandParent { get; set; }

    [Parameter] public string? SwitcherIcon { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public TreeViewSelectionMode SelectionMode { get; set; } = TreeViewSelectionMode.SingleItem;

    [Parameter] public IEnumerable<TItem>? DataSource { get; set; }

    [Parameter] public string[]? ExpandedKeys { get; set; }

    [Parameter] public Func<TreeViewNode<TItem>, string>? TitleExpression { get; set; }

    [Parameter] public Func<TreeViewNode<TItem>, string>? KeyExpression { get; set; }

    [Parameter] public Func<TreeViewNode<TItem>, string>? IconExpression { get; set; }

    [Parameter] public Func<TreeViewNode<TItem>, IEnumerable<TItem>>? ChildrenExpression { get; set; }

    [Parameter] public Func<TreeViewNode<TItem>, bool>? DisabledExpression { get; set; }
    
    [Parameter] public TreeViewCanDropHandler<TItem>? CanDropBelowExpression { get; set; }
    
    [Parameter] public TreeViewCanDropHandler<TItem>? CanDropInsideExpression { get; set; }

    [Parameter] public RenderFragment<TreeViewNode<TItem>>? IndentTemplate { get; set; }

    [Parameter] public RenderFragment<(TreeViewNode<TItem> Node, int IndentLevel)>? IndentWithLevelTemplate { get; set; }

    [Parameter] public RenderFragment<TreeViewNode<TItem>>? TitleTemplate { get; set; }

    [Parameter] public RenderFragment<TreeViewNode<TItem>>? TitleIconTemplate { get; set; }

    [Parameter] public RenderFragment<TreeViewNode<TItem>>? SwitcherIconTemplate { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnClick { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDblClick { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnContextMenu { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDragStart { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDragEnter { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDragLeave { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDrop { get; set; }

    [Parameter] public EventCallback<TreeViewEventArgs<TItem>> OnDragEnd { get; set; }

    [Parameter] public EventCallback<string[]> ExpandedKeysChanged { get; set; }

    [Parameter] public EventCallback<(string[] ExpandedKeys, TreeViewNode<TItem> Node, bool Expanded)> OnExpand { get; set; }

    public IObservableCache<TreeViewNode<TItem>, long> NodesById => nodesById;

    public IObservableCache<TreeViewNode<TItem>, long> SelectedItemsById { get; }

    public IObservableCache<TreeViewNode<TItem>, string> NodesByKey { get; }

    internal IDictionary<(long, long), TreeViewDragDropInfo> DragDropStateByNodeIds { get; } = new Dictionary<(long, long), TreeViewDragDropInfo>();

    internal bool IsCtrlKeyDown { get; private set; }

    internal bool IsShiftKeyDown { get; private set; }


    internal List<TreeViewNode<TItem>> ChildNodes { get; } = new();
    
    internal TreeViewNode<TItem>? DragDropNode { get; private set; }
    internal TreeViewNode<TItem>? DragDropTargetContainerNode { get; private set; }
    internal TreeViewNode<TItem>? DragDropTargetBelowNode { get; private set; }
    internal TreeViewNode<TItem>? DragDropTargetNode { get; private set; }

    [Inject] private IDomEventListener DomEventListener { get; init; } = null!;

    [Inject] private IJsPoeBlazorUtils JsPoeBlazorUtils { get; init; } = null!;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        DomEventListener?.Dispose();
        return base.DisposeAsync();
    }

    internal void AddChildNode(TreeViewNode<TItem> treeNode)
    {
        treeNode.NodeIndex = ChildNodes.Count;
        ChildNodes.Add(treeNode);
    }

    internal void AddNode(TreeViewNode<TItem> treeNode)
    {
        nodesById.AddOrUpdate(treeNode);
    }

    internal void RemoveNode(TreeViewNode<TItem> treeNode)
    {
        nodesById.Remove(treeNode);
    }

    internal async Task OnNodeExpand(TreeViewNode<TItem> node, bool expanded, MouseEventArgs args)
    {
        var expandedKeys = nodesById.Items.Where(x => x.Expanded).Select(x => x.Key).ToArray();
        if (ExpandedKeysChanged.HasDelegate)
        {
            await ExpandedKeysChanged.InvokeAsync(expandedKeys);
        }

        if (OnExpand.HasDelegate)
        {
            await OnExpand.InvokeAsync((expandedKeys, node, expanded));
        }

        if (AutoExpandParent && expanded)
        {
            node.ParentNode?.Expand(true);
        }
    }

    internal async Task SetSelection(HashSet<TreeViewNode<TItem>> selectedNodes, bool preserveSelection = false)
    {
        var existingSelection = preserveSelection ? SelectedItemsById.Items.ToHashSet() : new HashSet<TreeViewNode<TItem>>();
        foreach (var node in NodesById.Items)
        {
            var shouldBeSelected = selectedNodes.Contains(node) || existingSelection.Contains(node);

            if (shouldBeSelected == node.Selected)
            {
                continue;
            }

            node.Selected = shouldBeSelected;
            await node.SelectedChanged.InvokeAsync(shouldBeSelected);
        }
    }
    
    internal void SetDragTargetNode(TreeViewNode<TItem>? node)
    {
        if (ReferenceEquals(DragDropTargetNode, node))
        {
            return;
        }

        DragDropTargetNode = node;
    }
    
    internal void SetDragDropTargetContainerNode(TreeViewNode<TItem>? node)
    {
        if (ReferenceEquals(DragDropTargetContainerNode, node))
        {
            return; 
        }

        DragDropTargetContainerNode = node;
    }
    
    internal void SetDragDropTargetBelowNode(TreeViewNode<TItem>? node)
    {
        if (ReferenceEquals(DragDropTargetBelowNode, node))
        {
            return;
        }

        DragDropTargetBelowNode = node;
    }

    internal void NotifyDragStart(TreeViewNode<TItem> node)
    {
        DragDropStateByNodeIds.Clear();
        DragDropNode = node;
    }
    
    internal void NotifyDragEnd()
    {
        DragDropStateByNodeIds.Clear();
        DragDropNode = null;
        DragDropTargetNode = null;
        DragDropTargetBelowNode = null;
        DragDropTargetContainerNode = null;
    }

    protected override void OnInitialized()
    {
        SetClassMapper();
        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            DomEventListener.AddShared<KeyboardEventArgs>("document", "keydown", DocumentOnKeyDown);
            DomEventListener.AddShared<KeyboardEventArgs>("document", "keyup", DocumentOnKeyUp);
            DomEventListener.AddShared<FocusEventArgs>("window", "blur", WindowOnBlur);
        }

        base.OnAfterRender(firstRender);
    }

    private void SetClassMapper()
    {
        classMapper
            .Add("ant-tree")
            .Add("outline-none")
            .If("ant-tree-icon-hide", () => ShowIcon)
            .If("ant-tree-block-node", () => BlockNode)
            .If("draggable-tree", () => Draggable)
            .If("ant-tree-unselectable", () => SelectionMode == TreeViewSelectionMode.Disabled);
    }
    
    private void DocumentOnKeyDown(KeyboardEventArgs eventArgs)
    {
        HandleCtrlKeyPress(eventArgs);
    }

    private void WindowOnBlur(FocusEventArgs eventArgs)
    {
        NotifyDragEnd();
    }
    
    private void DocumentOnKeyUp(KeyboardEventArgs eventArgs)
    {
        HandleCtrlKeyPress(eventArgs);
    }

    private Task AddToSelection(params TreeViewNode<TItem>[] selectedNodes)
    {
        return SetSelection(new HashSet<TreeViewNode<TItem>>(selectedNodes), preserveSelection: true);
    }

    private Task SetSelection(params TreeViewNode<TItem>[] selectedNodes)
    {
        return SetSelection(new HashSet<TreeViewNode<TItem>>(selectedNodes), preserveSelection: false);
    }

    private async Task HandleContextMenu(MouseEventArgs args)
    {
        if (OnContextMenu.HasDelegate)
        {
            await OnContextMenu.InvokeAsync(new TreeViewEventArgs<TItem>(this)
            {
                OriginalEvent = args
            });
        }
    }

    private void HandleCtrlKeyPress(KeyboardEventArgs eventArgs)
    {
        IsCtrlKeyDown = eventArgs.CtrlKey || eventArgs.MetaKey;
        IsShiftKeyDown = eventArgs.ShiftKey;
    }

    private async Task HandleOnDrop(DragEventArgs e)
    {
    }

    private async Task HandleDragEnter(DragEventArgs e)
    {
    } 
    
    private async Task HandleDragEnd(DragEventArgs e)
    {
        NotifyDragEnd();
    }

    private async Task HandleDragOver(DragEventArgs e)
    {
    }
    
    private async Task HandleMouseLeave(MouseEventArgs e)
    {
    }

    private async Task HandleKeyDown(KeyboardEventArgs eventArgs)
    {
        if (eventArgs.Repeat)
        {
            return;
        }

        switch (SelectionMode)
        {
            case TreeViewSelectionMode.SingleItem or TreeViewSelectionMode.MultipleItems:
            {
                switch (eventArgs.Code)
                {
                    case "ArrowUp":
                    case "ArrowDown":
                    case "Escape":
                        break;
                    default:
                        return;
                }

                if (lastSelectedItem == null)
                {
                    //require origin item
                    return;
                }

                var allNodes = NodesById
                    .Items
                    .OrderBy(x => x.TreeLevel)
                    .ThenBy(x => x.NodeIndex)
                    .ToList();

                var originIndex = allNodes.IndexOf(lastSelectedItem);
                if (originIndex < 0)
                {
                    return;
                }

                if (eventArgs.Code == "ArrowUp")
                {
                    var previousNode = allNodes.ElementAtOrDefault(originIndex - 1);
                    if (previousNode != null)
                    {
                        if (eventArgs.ShiftKey && SelectionMode is TreeViewSelectionMode.MultipleItems)
                        {
                            await AddToSelection(previousNode);
                        }
                        else
                        {
                            await SetSelection(previousNode);
                        }

                        await ScrollElementIntoViewSafe(previousNode);
                    }
                }
                else if (eventArgs.Code == "ArrowDown")
                {
                    var nextNode = allNodes.ElementAtOrDefault(originIndex + 1);
                    if (nextNode != null)
                    {
                        if (eventArgs.ShiftKey && SelectionMode is TreeViewSelectionMode.MultipleItems)
                        {
                            await AddToSelection(nextNode);
                        }
                        else
                        {
                            await SetSelection(nextNode);
                        }

                        await ScrollElementIntoViewSafe(nextNode);
                    }
                }
                else if (eventArgs.Code == "Escape")
                {
                    await SetSelection();
                }

                break;
            }
        }
    }

    private async Task ScrollElementIntoViewSafe(TreeViewNode<TItem> node)
    {
        try
        {
            await JsPoeBlazorUtils.ScrollElementIntoView(node.ElementRef);
        }
        catch (Exception)
        {
            //there is a chance that element is already removed at this point
        }
    }
}