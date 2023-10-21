using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// This class wraps BlazorContentPresenter and is needed only inside WPF-infrastructure
/// Main reason why it is not a "normal" Razor component is to make it internal, which is not supported as of October 2023
/// </summary>
internal sealed class BlazorContentPresenterWrapper : ReactiveComponentBase
{
    [Parameter] public object Content { get; set; }
    
    public object ViewTypeKey { get; init; }
    
    public Type ViewType { get; init; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        var seq = 0;
        builder.OpenComponent<BlazorContentPresenter>(seq++);
        builder.AddAttribute(seq++, "Content", Content);
        builder.AddAttribute(seq++, "ViewTypeKey", ViewTypeKey);
        builder.AddAttribute(seq++, "ViewType", ViewType);
        builder.CloseComponent();
    }
}