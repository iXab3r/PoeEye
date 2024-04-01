using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeCheckbox<TItem> : BlazorReactiveComponent
{
    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    protected ClassMapper ClassMapper { get; } = new();

    [Parameter] public EventCallback<MouseEventArgs> OnCheckBoxClick { get; set; }

    private void SetClassMap()
    {
        ClassMapper
            .Add("ant-tree-checkbox")
            .If("ant-tree-checkbox-checked", () => SelfNode.Checked)
            .If("ant-tree-checkbox-indeterminate", () => SelfNode.Indeterminate)
            .If("ant-tree-checkbox-disabled", () => SelfNode.Disabled || SelfNode.DisableCheckbox);
    }

    protected override void OnInitialized()
    {
        SetClassMap();
        base.OnInitialized();
    }

    private async Task OnClick(MouseEventArgs args)
    {
        if (OnCheckBoxClick.HasDelegate)
        {
            await OnCheckBoxClick.InvokeAsync(args);
        }
    }
}