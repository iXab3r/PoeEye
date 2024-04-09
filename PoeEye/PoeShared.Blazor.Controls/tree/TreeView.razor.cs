// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntDesign;
using AntDesign.JsInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls;

public partial class TreeView<TItem> : BlazorReactiveComponent
{
    internal List<TreeViewNode<TItem>> allNodes = new();

    private string[] checkedKeys = Array.Empty<string>();

    private readonly ConcurrentDictionary<long, TreeViewNode<TItem>> checkedNodes = new();
    private Dictionary<long, TreeViewNode<TItem>> SelectedNodesDictionary { get; set; } = new();
    private bool hasSetShowLeafIcon;
    private bool showLeafIcon;
    private bool showLine;
    private readonly ClassMapper classMapper = new();
    

    [Parameter]
    public bool ShowExpand { get; set; } = true;

    [Parameter]
    public bool ShowLine
    {
        get => showLine;
        set
        {
            showLine = value;
            if (!hasSetShowLeafIcon)
            {
                ShowLeafIcon = showLine;
            }
        }
    }

    [Parameter]
    public bool ShowIcon { get; set; }

    [Parameter]
    public bool BlockNode { get; set; }

    [Parameter]
    public bool Draggable { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ShowLeafIcon
    {
        get => showLeafIcon;
        set
        {
            showLeafIcon = value;
            hasSetShowLeafIcon = true;
        }
    }

    [Parameter]
    public string SwitcherIcon { get; set; }

    [Parameter] public RenderFragment Nodes { get; set; }

    [Parameter] public RenderFragment ChildContent { get; set; }

    internal List<TreeViewNode<TItem>> ChildNodes { get; set; } = new();

    [Parameter]
    public bool Selectable { get; set; } = true;

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter] public string[] DefaultSelectedKeys { get; set; }

    [Parameter]
    public string SelectedKey { get; set; }

    [Parameter]
    public EventCallback<string> SelectedKeyChanged { get; set; }

    [Parameter]
    public TreeViewNode<TItem> SelectedNode { get; set; }

    [Parameter] public EventCallback<TreeViewNode<TItem>> SelectedNodeChanged { get; set; }

    [Parameter]
    public TItem SelectedData { get; set; }

    [Parameter] public EventCallback<TItem> SelectedDataChanged { get; set; }

    [Parameter]
    public string[] SelectedKeys { get; set; }

    [Parameter] public EventCallback<string[]> SelectedKeysChanged { get; set; }

    [Parameter]
    public TreeViewNode<TItem>[] SelectedNodes { get; set; }

    [Parameter]
    public TItem[] SelectedDatas { get; set; }

    [Parameter]
    public bool Checkable { get; set; }

    [Parameter]
    public bool CheckStrictly { get; set; }

    [Parameter]
    public string[] CheckedKeys
    {
        get => checkedKeys;
        set
        {
            if (value == null)
            {
                checkedKeys = Array.Empty<string>();
            }
            else if (!value.SequenceEqual(checkedKeys))
            {
                checkedKeys = value;
            }
        }
    }

    [Parameter]
    public EventCallback<string[]> CheckedKeysChanged { get; set; }

    [Parameter]
    public string[] DefaultCheckedKeys { get; set; }

    public string[] DisableCheckKeys { get; set; }

    [Parameter]
    public IEnumerable<TItem> DataSource { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> TitleExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> KeyExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, string> IconExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, bool> IsLeafExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, IEnumerable<TItem>> ChildrenExpression { get; set; }

    [Parameter]
    public Func<TreeViewNode<TItem>, bool> DisabledExpression { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnNodeLoadDelayAsync { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnClick { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnDblClick { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnContextMenu { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnCheck { get; set; }

    [Parameter] 
    public EventCallback<TreeViewEventArgs<TItem>> OnSelect { get; set; }

    [Parameter] 
    public EventCallback<TreeViewEventArgs<TItem>> OnUnselect { get; set; }

    [Parameter]
    public EventCallback<TreeViewEventArgs<TItem>> OnExpandChanged { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> IndentTemplate { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> TitleTemplate { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> TitleIconTemplate { get; set; }

    [Parameter]
    public RenderFragment<TreeViewNode<TItem>> SwitcherIconTemplate { get; set; }

    internal TreeViewNode<TItem> DragItem { get; set; }

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

    [Inject] private IDomEventListener DomEventListener { get; set; }

    internal bool IsCtrlKeyDown { get; set; }

    private void SetClassMapper()
    {
        classMapper
            .Add("ant-tree")
            .If("ant-tree-show-line", () => ShowLine)
            .If("ant-tree-icon-hide", () => ShowIcon)
            .If("ant-tree-block-node", () => BlockNode)
            .If("draggable-tree", () => Draggable)
            .If("ant-tree-unselectable", () => !Selectable);
    }

    internal void AddChildNode(TreeViewNode<TItem> treeNode)
    {
        treeNode.NodeIndex = ChildNodes.Count;
        ChildNodes.Add(treeNode);
    }

    internal void AddNode(TreeViewNode<TItem> treeNode)
    {
        allNodes.Add(treeNode);
    }

    internal void SelectedNodeAdd(TreeViewNode<TItem> treeNode)
    {
        if (SelectedNodesDictionary.ContainsKey(treeNode.NodeId) == false)
        {
            SelectedNodesDictionary.Add(treeNode.NodeId, treeNode);
        }

        if (OnSelect.HasDelegate)
        {
            OnSelect.InvokeAsync(new TreeViewEventArgs<TItem>(this, treeNode));
        }
    }

    internal void SelectedNodeRemove(TreeViewNode<TItem> treeNode)
    {
        if (SelectedNodesDictionary.ContainsKey(treeNode.NodeId))
        {
            SelectedNodesDictionary.Remove(treeNode.NodeId);
        }

        if (OnUnselect.HasDelegate)
        {
            OnUnselect.InvokeAsync(new TreeViewEventArgs<TItem>(this, treeNode));
        }
    }

    public void DeselectAll()
    {
        foreach (var item in SelectedNodesDictionary.Select(x => x.Value).ToList())
        {
            item.SetSelected(false);
        }
    }

    private async Task HandleContextMenu(MouseEventArgs args)
    {
        if (OnContextMenu.HasDelegate)
        {
            await OnContextMenu.InvokeAsync(new TreeViewEventArgs<TItem>(this, null, args));
        }
    }

    internal void UpdateBindData()
    {
        if (SelectedNodesDictionary.Count == 0)
        {
            SelectedKey = null;
            SelectedNode = null;
            SelectedData = default;
            SelectedKeys = Array.Empty<string>();
            SelectedNodes = Array.Empty<TreeViewNode<TItem>>();
            SelectedDatas = Array.Empty<TItem>();
        }
        else
        {
            var selectedFirst = SelectedNodesDictionary.FirstOrDefault();
            
            SelectedKey = selectedFirst.Value?.Key;
            SelectedNode = selectedFirst.Value;
            SelectedData = selectedFirst.Value == default ? default : selectedFirst.Value.DataItem;
            SelectedKeys = SelectedNodesDictionary.Select(x => x.Value.Key).ToArray();
            SelectedNodes = SelectedNodesDictionary.Select(x => x.Value).ToArray();
            SelectedDatas = SelectedNodesDictionary.Select(x => x.Value.DataItem).ToArray();
        }

        if (SelectedKeyChanged.HasDelegate)
        {
            SelectedKeyChanged.InvokeAsync(SelectedKey);
        }

        if (SelectedNodeChanged.HasDelegate)
        {
            SelectedNodeChanged.InvokeAsync(SelectedNode);
        }

        if (SelectedDataChanged.HasDelegate)
        {
            SelectedDataChanged.InvokeAsync(SelectedData);
        }

        if (SelectedKeysChanged.HasDelegate)
        {
            SelectedKeysChanged.InvokeAsync(SelectedKeys);
        }
    }

    public void CheckAll()
    {
        foreach (var item in ChildNodes)
        {
            item.SetChecked(true);
        }
    }

    public void UncheckAll()
    {
        foreach (var item in ChildNodes)
        {
            item.SetChecked(false);
        }
    }

    public void SelectAll()
    {
        foreach (var item in ChildNodes)
        {
            item.SetSelected(true);
        }
    }

    internal void AddOrRemoveCheckNode(TreeViewNode<TItem> treeNode)
    {
        var old = checkedKeys;
        if (treeNode.Checked)
        {
            checkedNodes.TryAdd(treeNode.NodeId, treeNode);
        }
        else
        {
            checkedNodes.TryRemove(treeNode.NodeId, out var _);
        }

        checkedKeys = checkedNodes.OrderBy(x => x.Value.NodeId).Select(x => x.Value.Key).ToArray();

        if (!old.SequenceEqual(checkedKeys) && CheckedKeysChanged.HasDelegate)
        {
            CheckedKeysChanged.InvokeAsync(checkedKeys);
        }
    }

    protected override void OnInitialized()
    {
        SetClassMapper();
        base.OnInitialized();
    }

    protected override Task OnAfterFirstRenderAsync()
    {
        DefaultCheckedKeys?.ForEach(k =>
        {
            var node = allNodes.FirstOrDefault(x => x.Key == k);
            if (node != null)
            {
                node.SetCheckedDefault(true);
            }
        });

        DefaultSelectedKeys?.ForEach(k =>
        {
            var node = allNodes.FirstOrDefault(x => x.Key == k);
            if (node != null)
            {
                node.SetSelected(true);
            }
        });

        return base.OnAfterFirstRenderAsync();
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var isChanged = parameters.IsParameterChanged(nameof(SelectedKeys), SelectedKeys) ||
                        parameters.IsParameterChanged(nameof(CheckedKeys), CheckedKeys) ||
                        parameters.IsParameterChanged(nameof(ExpandedKeys), ExpandedKeys);

        await base.SetParametersAsync(parameters);

        if (isChanged)
        {
            UpdateState();
        }
    }

    public TreeViewNode<TItem> GetNode(string key)
    {
        return allNodes.FirstOrDefault(x => x.Key == key);
    }

    public TreeViewNode<TItem> FindFirstOrDefaultNode(Func<TreeViewNode<TItem>, bool> predicate, bool recursive = true)
    {
        foreach (var child in ChildNodes)
        {
            if (predicate != null && predicate.Invoke(child))
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

    public void ExpandAll(Func<TreeViewNode<TItem>, bool> predicate = null, bool recursive = true)
    {
        if (predicate != null)
        {
            _ = FindFirstOrDefaultNode(predicate, recursive).ExpandAll();
        }
        else
        {
            ChildNodes.ForEach(node => _ = node.ExpandAll());
        }
    }

    public void CollapseAll(Func<TreeViewNode<TItem>, bool> predicate = null, bool recursive = true)
    {
        if (predicate != null)
        {
            _ = FindFirstOrDefaultNode(predicate, recursive).CollapseAll();
        }
        else
        {
            ChildNodes.ForEach(node => _ = node.CollapseAll());
        }
    }

    internal async Task OnNodeExpand(TreeViewNode<TItem> node, bool expanded, MouseEventArgs args)
    {
        var expandedKeys = allNodes.Where(x => x.Expanded).Select(x => x.Key).ToArray();
        if (OnNodeLoadDelayAsync.HasDelegate && expanded)
        {
            node.SetLoading(true);
            await OnNodeLoadDelayAsync.InvokeAsync(new TreeViewEventArgs<TItem>(this, node, args));
            node.SetLoading(false);
            StateHasChanged();
        }

        if (OnExpandChanged.HasDelegate)
        {
            await OnExpandChanged.InvokeAsync(new TreeViewEventArgs<TItem>(this, node, args));
        }

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

    private void HandleCtrlKeyPress(KeyboardEventArgs eventArgs)
    {
        IsCtrlKeyDown = eventArgs.CtrlKey || eventArgs.MetaKey;
    }

    public override ValueTask DisposeAsync()
    {
        DomEventListener?.Dispose();
        return base.DisposeAsync();
    }

    private void UpdateState()
    {
        foreach (var node in allNodes)
        {
            node.SetSingleNodeChecked(CheckedKeys?.Contains(node.Key) == true);
            node.Selected = SelectedKeys?.Contains(node.Key) == true;
            node.Expanded = ExpandedKeys?.Contains(node.Key) == true;
        }
    }
}