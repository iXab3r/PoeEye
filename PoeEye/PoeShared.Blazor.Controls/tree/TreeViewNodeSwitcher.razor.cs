using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeSwitcher<TItem> : ComponentBase
{
    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    private bool IsSwitcherOpen => SelfNode.Expanded && !SelfNode.IsLeaf;

    private bool IsSwitcherClose => !SelfNode.Expanded && !SelfNode.IsLeaf;

    protected ClassMapper ClassMapper { get; } = new();

    [Parameter] public EventCallback<MouseEventArgs> OnSwitcherClick { get; set; }

    private void SetClassMap()
    {
        ClassMapper
            .Add("ant-tree-switcher")
            .If("ant-tree-switcher-noop", () => SelfNode.IsLeaf)
            .If("ant-tree-switcher_open", () => IsSwitcherOpen)
            .If("ant-tree-switcher_close", () => IsSwitcherClose);
    }

    protected override void OnInitialized()
    {
        SetClassMap();
        base.OnInitialized();
    }

    private async Task OnClick(MouseEventArgs args)
    {
        if (OnSwitcherClick.HasDelegate)
        {
            await OnSwitcherClick.InvokeAsync(args);
        }
    }
}