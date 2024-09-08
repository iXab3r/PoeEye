using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.JsInterop;
using DynamicData;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeView<TItem> : BlazorReactiveComponent
{
    private readonly SourceCache<TreeViewNode<TItem>, long> nodesById = new(x => x.NodeId);

    private readonly ClassMapper classMapper = new();

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
            .AsObservableCache()
            .AddTo(Anchors);
        
        SelectedItemsById = nodesById
            .Connect()
            .AutoRefreshOnObservable(x => x.WhenAnyValue(y => y.Selected))
            .Filter(x => x.Selected)
            .AsObservableCache()
            .AddTo(Anchors);
    }

    [Parameter]
    public bool ShowExpand { get; set; } = true;
    
    [Parameter]
    public bool? ShowExpandedByDefault { get; set; } 

    [Parameter]
    public bool ShowIcon { get; set; }

    [Parameter]
    public bool BlockNode { get; set; }

    [Parameter]
    public bool Draggable { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string SwitcherIcon { get; set; }

    [Parameter] public RenderFragment Nodes { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }

    [Parameter]
    public bool Selectable { get; set; } = true;

    [Parameter]
    public IEnumerable<TItem> DataSource { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> TitleExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> KeyExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> IconExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, IEnumerable<TItem>> ChildrenExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, bool> DisabledExpression { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnClick { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDblClick { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnContextMenu { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> IndentTemplate { get; set; }

    [Parameter]
    public RenderFragment<(TreeViewNode<TItem> Node, int IndentLevel)> IndentWithLevelTemplate { get; set; }
    
    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> TitleTemplate { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> TitleIconTemplate { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> SwitcherIconTemplate { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDragStart { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDragEnter { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDragLeave { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDrop { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDragEnd { get; set; }

    [Parameter]
    public string[] ExpandedKeys { get; set; }

    [Parameter] public EventCallback<string[]> ExpandedKeysChanged { get; set; }

    [Parameter] public EventCallback<(string[] ExpandedKeys, TreeViewNode<TItem> Node, bool Expanded)> OnExpand { get; set; }

    [Parameter] public bool AutoExpandParent { get; set; }

    public IObservableCache<TreeViewNode<TItem>, long> NodesById => nodesById;
    
    public IObservableCache<TreeViewNode<TItem>, long> SelectedItemsById { get; }

    public IObservableCache<TreeViewNode<TItem>, string> NodesByKey { get; }

    internal bool IsCtrlKeyDown { get; set; }
    
    internal TreeViewNode<TItem> DragItem { get; set; }
    
    internal List<TreeViewNode<TItem>> ChildNodes { get; set; } = new();

    [Inject] private IDomEventListener DomEventListener { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
    }

    public IEnumerable<TreeViewNode<TItem>> EnumerateChildren()
    {
        foreach (var childNode in ChildNodes)
        {
            foreach (var node in childNode.EnumerateChildrenAndSelf())
            {
                yield return node;
            }
        }
    }

    public override ValueTask DisposeAsync()
    {
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

    protected override void OnInitialized()
    {
        SetClassMapper();
        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            DomEventListener.AddShared<KeyboardEventArgs>("document", "keydown", OnKeyDown);
            DomEventListener.AddShared<KeyboardEventArgs>("document", "keyup", OnKeyUp);
        }

        base.OnAfterRender(firstRender);
    }

    protected virtual void OnKeyDown(KeyboardEventArgs eventArgs)
    {
        HandleCtrlKeyPress(eventArgs);
    }

    protected virtual void OnKeyUp(KeyboardEventArgs eventArgs)
    {
        HandleCtrlKeyPress(eventArgs);
    }
    
    private void SetClassMapper()
    {
        classMapper
            .Add("ant-tree")
            .Add("drop-target")
            .Add("drop-container")
            .If("ant-tree-icon-hide", () => ShowIcon)
            .If("ant-tree-block-node", () => BlockNode)
            .If("draggable-tree", () => Draggable)
            .If("ant-tree-unselectable", () => !Selectable);
    }

    private async Task HandleContextMenu(MouseEventArgs args)
    {
        if (OnContextMenu.HasDelegate)
        {
            await OnContextMenu.InvokeAsync(new TreeViewEventArgs<TItem>(this, null, args));
        }
    }

    private void HandleCtrlKeyPress(KeyboardEventArgs eventArgs)
    {
        IsCtrlKeyDown = eventArgs.CtrlKey || eventArgs.MetaKey;
    }
    
    private async Task HandleOnDrop(DragEventArgs e)
    {
      
    }
    
    private async Task HandleDragEnter(DragEventArgs e)
    {
        
    }

    private async Task HandleDragOver(DragEventArgs e)
    {
        
    }
}