using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNode<TItem> : BlazorReactiveComponent
{
    private static readonly Binder<TreeViewNode<TItem>> Binder = new();

    private readonly ClassMapper classMapper = new();

    private bool disableCheckbox;
    private bool disabled;
    private string icon;
    private string key;
    private string title;

    static TreeViewNode()
    {
        Binder.Bind(x => x.Expanded && !x.IsLeaf)
            .To(x => x.IsSwitcherOpen);

        Binder.Bind(x => !x.Expanded && !x.IsLeaf)
            .To(x => x.IsSwitcherClose);
    }

    public TreeViewNode()
    {
        NodeId = TreeViewHelper.GetNextNodeId();

        ChangeTrackers.Add(this.WhenAnyValue(x => x.IsVisible));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.IsTargetBottom));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.IsTargetContainer));

        Binder.Attach(this).AddTo(Anchors);
    }

    [CascadingParameter(Name = "Tree")] public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "Node")] public TreeViewNode<TItem>? ParentNode { get; set; }

    [Parameter] public RenderFragment? Nodes { get; set; }

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
    public bool Disabled
    {
        get => disabled || (ParentNode?.Disabled ?? false);
        set => disabled = value;
    }

    [Parameter] public bool Selected { get; set; }

    [Parameter] public EventCallback<bool> SelectedChanged { get; set; }

    [Parameter] public bool Expanded { get; set; }

    [Parameter] public bool Indeterminate { get; set; }

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

    public bool IsSwitcherOpen { get; private set; }

    public bool IsSwitcherClose { get; private set; }

    public bool IsDragTarget { get; private set; }

    public int TreeLevel => (ParentNode?.TreeLevel ?? -1) + 1;

    public bool IsTargetBottom { get; private set; }

    public bool IsTargetContainer { get; private set; }

    public bool IsLastNode => NodeIndex == (ParentNode?.ChildNodes.Count ?? TreeComponent?.ChildNodes.Count) - 1;

    internal List<TreeViewNode<TItem>> ChildNodes { get; set; } = new();

    internal int NodeIndex { get; set; }

    internal long NodeId { get; private set; }

    private bool IsVisible
    {
        get
        {
            if (Hidden)
            {
                return false;
            }

            if (ParentNode == null)
            {
                return true;
            }

            if (ParentNode.Expanded == false)
            {
                return false;
            }

            return ParentNode.IsVisible;
        }
    }

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

    public void RemoveNode()
    {
        GetParentChildNodes().Remove(this);
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


    public IList<TItem> GetParentChildDataItems()
    {
        if (ParentNode != null)
        {
            return ParentNode.ChildDataItems;
        }

        return TreeComponent.DataSource as IList<TItem> ?? TreeComponent.DataSource.ToList();
    }

    public void AddChildNode(TItem dataItem)
    {
        ChildDataItems.Add(dataItem);
    }

    public async Task AddNextNode(TItem dataItem)
    {
        var parentChildDataItems = GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(DataItem);
        parentChildDataItems.Insert(index + 1, dataItem);

        await AddNodeAndSelect(dataItem);
    }

    public void Remove()
    {
        var parentChildDataItems = GetParentChildDataItems();
        parentChildDataItems.Remove(DataItem);
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

    internal void AddNode(TreeViewNode<TItem> treeNode)
    {
        treeNode.NodeIndex = ChildNodes.Count;
        ChildNodes.Add(treeNode);
        IsLeaf = false;
    }

    internal void SetTargetBottom(bool value)
    {
        if (IsTargetBottom == value)
        {
            return;
        }

        IsTargetBottom = value;
    }

    internal void SetDragTarget(bool value)
    {
        if (IsDragTarget == value)
        {
            return;
        }

        IsDragTarget = value;
    }

    internal void SetParentTargetContainer(bool value)
    {
        if (ParentNode == null)
        {
            return;
        }

        if (ParentNode.IsTargetContainer == value)
        {
            return;
        }

        ParentNode.IsTargetContainer = value;
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

    public override async ValueTask DisposeAsync()
    {
        TreeComponent.RemoveNode(this);
        await base.DisposeAsync();
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
    }

    private List<TreeViewNode<TItem>> GetParentChildNodes()
    {
        return ParentNode?.ChildNodes ?? TreeComponent.ChildNodes;
    }

    private void SetTreeViewNodeClassMapper()
    {
        classMapper
            .Add("ant-tree-treenode")
            .If("ant-tree-treenode-disabled", () => Disabled)
            .If("ant-tree-treenode-switcher-open", () => SwitcherOpen)
            .If("ant-tree-treenode-switcher-close", () => SwitcherClose)
            .If("ant-tree-treenode-checkbox-indeterminate", () => Indeterminate)
            .If("ant-tree-treenode-selected", () => Selected)
            .If("drop-target", () => IsDragTarget)
            .If("drag-over-gap-bottom", () => IsDragTarget && IsTargetBottom)
            .If("drag-over", () => IsDragTarget && !IsTargetBottom)
            .If("drop-container", () => IsTargetContainer)
            .If("ant-tree-treenode-leaf-last", () => IsLastNode);
    }

    private async Task SwitchAllNodes(TreeViewNode<TItem> node, bool expanded)
    {
        await node.Expand(expanded);
        node.ChildNodes.ForEach(n => _ = SwitchAllNodes(n, expanded));
    }

    private async Task OnSwitcherClick(MouseEventArgs args)
    {
        Expanded = !Expanded;

        var tree = TreeComponent;
        if (tree != null)
        {
            await tree.OnNodeExpand(this, Expanded, args);
        }
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
}