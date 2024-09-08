using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewNodeSwitcher<TItem> : BlazorReactiveComponent
{
    private static readonly Binder<TreeViewNodeSwitcher<TItem>> Binder = new();

    static TreeViewNodeSwitcher()
    {
    }

    public TreeViewNodeSwitcher()
    {
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsLeaf));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsSwitcherOpen));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.SelfNode.IsSwitcherOpen));
        ChangeTrackers.Add(this.WhenAnyValue(x => x.TreeComponent.ShowExpand));
        
        Binder.Attach(this).AddTo(Anchors);
    }

    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    [Parameter] public EventCallback<MouseEventArgs> OnSwitcherClick { get; set; }
    
    
    protected ClassMapper ClassMapper { get; } = new();

    protected override void OnInitialized()
    {
        ClassMapper
            .Add("ant-tree-switcher")
            .If("ant-tree-switcher-noop", () => SelfNode.IsLeaf)
            .If("ant-tree-switcher_open", () => SelfNode.IsSwitcherOpen)
            .If("ant-tree-switcher_close", () => SelfNode.IsSwitcherClose);
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