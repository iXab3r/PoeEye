#nullable enable

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PoeShared.Blazor.Controls;

public partial class BootstrapTooltip : BlazorReactiveComponent
{
    public static readonly string JsFilePath = "./_content/PoeShared.Blazor.Controls/js/BootstrapTooltip.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    /// <summary>
    /// Sets the text of the tooltip.
    /// </summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = String.Empty;

    /// <summary>
    /// Enables HTML Content in the tooltip. 
    /// <para>
    /// Default is <c>true</c>.
    /// </para>
    /// </summary>
    [Parameter]
    public bool Html { get; set; } = true;

    /// <summary>
    /// Sets the content which the tooltip should wrap around.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Sets the default positioning of the tooltip.
    /// <para>
    /// Default is <see cref="BootstrapTooltipPlacement.Top"/>
    /// </para>
    /// </summary>
    [Parameter]
    public BootstrapTooltipPlacement Placement { get; set; } = BootstrapTooltipPlacement.Auto;

    /// <summary>
    /// Sets the bootstrap custom class property
    /// 
    /// <para>
    /// Default is <c>string.Empty</c>
    /// </para>
    /// 
    /// <para>
    /// Requires at least Bootstrap 5.2 or later.
    /// </para>
    /// </summary>
    [Parameter]
    public string CustomClass { get; set; } = string.Empty;

    /// <summary>
    /// Apply a CSS fade transition to the tooltip
    /// <para>
    /// Default is <c>true</c>
    /// </para>
    /// </summary>
    [Parameter]
    public bool Animation { get; set; } = true;

    /// <summary>
    /// Delay showing and hiding the tooltip (ms)—doesn’t apply to manual trigger type. If a number is supplied, delay is applied to both hide/show.   
    /// </summary>
    [Parameter]
    public int Delay { get; set; } = 0;

    [Parameter] public string Container { get; set; } = string.Empty;
    [Parameter] public string Selector { get; set; } = string.Empty;
    [Parameter] public string Template { get; set; } = "<div class='tooltip' role='tooltip'><div class='tooltip-arrow'></div><div class='tooltip-inner'></div></div>";
    [Parameter] public BootstrapTooltipTrigger[] Trigger { get; set; } = new BootstrapTooltipTrigger[] {BootstrapTooltipTrigger.Hover};

    [Parameter]
    public BootstrapTooltipPlacement[] FallbackPlacements { get; set; } = new BootstrapTooltipPlacement[] {BootstrapTooltipPlacement.Top, BootstrapTooltipPlacement.Right, BootstrapTooltipPlacement.Bottom, BootstrapTooltipPlacement.Left};

    [Parameter] public string Boundary { get; set; } = "clippingParents";
    [Parameter] public bool Sanitize { get; set; } = true;

    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> AllOtherAttributes { get; set; } = new Dictionary<string, object>();

    public BootstrapTooltip()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            this.jsRuntime.InvokeAsync<IJSObjectReference>("import", JsFilePath).AsTask());
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            var module = await GetModuleAsync();

            await module.InvokeVoidAsync("destroy", ComponentId.ToString());
            await module.DisposeAsync();
        }
        catch (Exception e)
        {
            Log.Error("Exception on disposal of tooltip object", e);
        }
        await base.DisposeAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();
        await Setup();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsComponentRendered)
        {
            await Update();
        }
    }

    private async Task Setup()
    {
        var module = await GetModuleAsync();
        
        await module.InvokeVoidAsync("setup", new object[] {ComponentId.ToString(), OptionsToJson()});
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return await moduleTask.Value;
    }

    private string OptionsToJson()
    {
        dynamic tmp = new ExpandoObject();
        tmp.animation = Animation;
        tmp.delay = Delay;
        tmp.html = Html;
        tmp.placement = Placement.ToString().ToLower();
        tmp.template = Template;
        tmp.title = Title;
        tmp.trigger = String.Join(' ', Trigger.Distinct().Select(x => x.ToString().ToLower()));
        tmp.fallbackPlacements = FallbackPlacements.Select(x => x.ToString().ToLower()).ToArray();
        tmp.boundary = Boundary;
        tmp.customClass = CustomClass;
        tmp.sanitize = Sanitize;
        tmp.closest = false;

        if (String.IsNullOrWhiteSpace(Container))
        {
            tmp.container = false;
        }
        else
        {
            tmp.container = Container;
        }

        if (string.IsNullOrWhiteSpace(Selector))
        {
            tmp.selector = false;
        }
        else
        {
            tmp.selector = Selector;
        }

        return JsonSerializer.Serialize(tmp);
    }

    private async Task Update()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("update", new object[] {ComponentId.ToString(), OptionsToJson()});
    }
}