// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNode<TItem> : BlazorReactiveComponent
{
    private readonly ClassMapper classMapper = new();
    
    private bool disableCheckbox;
    private bool disabled;
    private bool dragTarget;
    private string icon;
    private bool isLeaf = true;
    private string key;
    private bool selected;
    private string title;

    public TreeViewNode()
    {
        NodeId = TreeViewHelper.GetNextNodeId();
    }

    [CascadingParameter(Name = "Tree")] public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "Node")] public TreeViewNode<TItem> ParentNode { get; set; }

    [Parameter] public RenderFragment Nodes { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }

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

    [Parameter]
    public bool Selected
    {
        get => selected;
        set
        {
            if (selected == value)
            {
                return;
            }

            SetSelected(value);
        }
    }

    [Parameter] public bool Loading { get; set; }

    [Parameter]
    public bool IsLeaf
    {
        get
        {
            if (TreeComponent.IsLeafExpression != null)
            {
                return TreeComponent.IsLeafExpression(this);
            }

            return isLeaf;
        }
        set
        {
            if (isLeaf == value)
            {
                return;
            }

            isLeaf = value;
            StateHasChanged();
        }
    }

    [Parameter] public bool Expanded { get; set; }

    [Parameter] public bool Indeterminate { get; set; }

    [Parameter] public bool Draggable { get; set; }

    [Parameter]
    public string Icon
    {
        get => TreeComponent.IconExpression != null ? TreeComponent.IconExpression(this) : icon;
        set => icon = value;
    }

    [Parameter] public RenderFragment<TreeViewNode<TItem>> IconTemplate { get; set; }

    [Parameter] public string SwitcherIcon { get; set; }

    [Parameter] public RenderFragment<TreeViewNode<TItem>> SwitcherIconTemplate { get; set; }

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

    [Parameter] public RenderFragment TitleTemplate { get; set; }

    [Parameter] public TItem DataItem { get; set; }

    internal bool RealDisplay
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

            return ParentNode.RealDisplay;
        }
    }

    internal bool DragTarget
    {
        get => dragTarget;
        set
        {
            if (dragTarget == value)
            {
                return;
            }

            dragTarget = value;
            StateHasChanged();
        }
    }

    internal List<TreeViewNode<TItem>> ChildNodes { get; set; } = new();

    internal bool HasChildNodes => ChildNodes?.Count > 0;

    public int TreeLevel => (ParentNode?.TreeLevel ?? -1) + 1;

    internal int NodeIndex { get; set; }

    internal bool IsLastNode => NodeIndex == (ParentNode?.ChildNodes.Count ?? TreeComponent?.ChildNodes.Count) - 1;

    internal long NodeId { get; private set; }

    internal bool IsTargetBottom { get; private set; }

    private bool IsTargetContainer { get; set; }

    private bool SwitcherOpen => Expanded && !IsLeaf;

    private bool SwitcherClose => !Expanded && !IsLeaf;

    public bool Matched { get; set; }

    [Parameter] public bool Hidden { get; set; }

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

    internal void AddNode(TreeViewNode<TItem> treeNode)
    {
        treeNode.NodeIndex = ChildNodes.Count;
        ChildNodes.Add(treeNode);
        IsLeaf = false;
    }

    public TreeViewNode<TItem> FindFirstOrDefaultNode(Func<TreeViewNode<TItem>, bool> predicate, bool recursive = true)
    {
        foreach (var child in ChildNodes)
        {
            if (predicate.Invoke(child))
            {
                return child;
            }

            if (recursive)
            {
                var find = child.FindFirstOrDefaultNode(predicate, recursive);
                if (find != null)
                {
                    return find;
                }
            }
        }

        return null;
    }

    public List<TreeViewNode<TItem>> GetParentNodes()
    {
        if (ParentNode != null)
        {
            return ParentNode.ChildNodes;
        }

        return TreeComponent.ChildNodes;
    }

    public TreeViewNode<TItem> GetPreviousNode()
    {
        var parentNodes = GetParentNodes();
        var index = parentNodes.IndexOf(this);
        if (index == 0)
        {
            return null;
        }

        return parentNodes[index - 1];
    }

    public TreeViewNode<TItem> GetNextNode()
    {
        var parentNodes = GetParentNodes();
        var index = parentNodes.IndexOf(this);
        if (index == parentNodes.Count - 1)
        {
            return null;
        }

        return parentNodes[index + 1];
    }

    public void SetSelected(bool value)
    {
        if (Disabled)
        {
            return;
        }

        if (selected == value)
        {
            return;
        }

        selected = value;
        if (value)
        {
            TreeComponent.SelectedNodeAdd(this);
        }
        else
        {
            TreeComponent.SelectedNodeRemove(this);
        }
        
        StateHasChanged();
    }

    internal void SetTargetBottom(bool value = false)
    {
        if (IsTargetBottom == value)
        {
            return;
        }

        IsTargetBottom = value;
        StateHasChanged();
    }

    internal void SetParentTargetContainer(bool value = false)
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
        ParentNode.StateHasChanged();
    }

    private List<TreeViewNode<TItem>> GetParentChildNodes()
    {
        return ParentNode?.ChildNodes ?? TreeComponent.ChildNodes;
    }

    public void RemoveNode()
    {
        GetParentChildNodes().Remove(this);
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
            .If("ant-tree-treenode-loading", () => Loading)
            .If("drop-target", () => DragTarget)
            .If("drag-over-gap-bottom", () => DragTarget && IsTargetBottom)
            .If("drag-over", () => DragTarget && !IsTargetBottom)
            .If("drop-container", () => IsTargetContainer)
            .If("ant-tree-treenode-leaf-last", () => IsLastNode);
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
        var tree = TreeComponent;
        if (tree != null)
        {
            await tree.OnNodeExpand(this, Expanded, new MouseEventArgs());
        }
    }

    internal async Task ExpandAll()
    {
        await SwitchAllNodes(this, true);
    }

    internal async Task CollapseAll()
    {
        await SwitchAllNodes(this, false);
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

    internal void SetLoading(bool loading)
    {
        Loading = loading;
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

    public async Task AddPreviousNode(TItem dataItem)
    {
        var parentChildDataItems = GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(DataItem);
        parentChildDataItems.Insert(index, dataItem);

        await AddNodeAndSelect(dataItem);
    }

    public void Remove()
    {
        var parentChildDataItems = GetParentChildDataItems();
        parentChildDataItems.Remove(DataItem);
    }

    public void MoveInto(TreeViewNode<TItem> treeNode)
    {
        if (treeNode == this || DataItem.Equals(treeNode.DataItem))
        {
            return;
        }

        var parentChildDataItems = GetParentChildDataItems();
        parentChildDataItems.Remove(DataItem);
        treeNode.AddChildNode(DataItem);
    }

    public void MoveUp()
    {
        var parentChildDataItems = GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(DataItem);
        if (index == 0)
        {
            return;
        }

        parentChildDataItems.RemoveAt(index);
        parentChildDataItems.Insert(index - 1, DataItem);
    }

    public void MoveDown()
    {
        var parentChildDataItems = GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(DataItem);
        if (index == parentChildDataItems.Count - 1)
        {
            return;
        }

        parentChildDataItems.RemoveAt(index);
        parentChildDataItems.Insert(index + 1, DataItem);
    }

    public void Downgrade()
    {
        var previousNode = GetPreviousNode();
        if (previousNode == null)
        {
            return;
        }

        var parentChildDataItems = GetParentChildDataItems();
        parentChildDataItems.Remove(DataItem);
        previousNode.AddChildNode(DataItem);
    }

    public void Upgrade()
    {
        if (ParentNode == null)
        {
            return;
        }

        var parentChildDataItems = ParentNode.GetParentChildDataItems();
        var index = parentChildDataItems.IndexOf(ParentNode.DataItem);
        Remove();
        parentChildDataItems.Insert(index + 1, DataItem);
    }

    private async Task AddNodeAndSelect(TItem dataItem)
    {
        var tn = ChildNodes.FirstOrDefault(treeNode => treeNode.DataItem.Equals(dataItem));
        if (tn != null)
        {
            await Expand(true);
            tn.SetSelected(true);
        }
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

        Selected = !string.IsNullOrEmpty(TreeComponent.SelectedKey) && string.Equals(Key, TreeComponent.SelectedKey);
        SetTreeViewNodeClassMapper();
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
}