using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor;

public sealed class ReactiveSection : BlazorReactiveComponent
{
    public ReactiveSection()
    {
        this.WhenAnyValue(x => x.Trackers)
            .Select(x => x ?? new ReactiveTrackerList())
            .Select(x => x.Merge())
            .Switch()
            .SubscribeAsync(x => Refresh(x))
            .AddTo(Anchors);
    }
    
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; }
    
    [Parameter] public ReactiveTrackerList Trackers { get; set; }

    [Parameter] public string AsElement { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)] public IEnumerable<KeyValuePair<string, object>> AdditionalAttributes { get; set; }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        var seq = 0;

        var hasAttribute = !string.IsNullOrEmpty(Id) ||
                           !string.IsNullOrEmpty(Class) ||
                           !string.IsNullOrEmpty(Style) ||
                           AdditionalAttributes != null;
        if (AsElement != null || hasAttribute)
        {
            builder.OpenElement(seq++, AsElement ?? "span");
        
            if (!string.IsNullOrEmpty(Id))
            {
                builder.AddAttribute(seq++, "id", Id);
            }
        
            if (!string.IsNullOrEmpty(Class))
            {
                builder.AddAttribute(seq++, "class", Class);
            }
        
            if (!string.IsNullOrEmpty(Style))
            {
                builder.AddAttribute(seq++, "style", Style);
            }
        
            if (AdditionalAttributes != null)
            {
                foreach (var attribute in AdditionalAttributes)
                {
                    builder.AddAttribute(seq++, attribute.Key, attribute.Value);
                }
            }
        
            if (ChildContent != null)
            {
                builder.AddContent(seq++, ChildContent);
            }
            builder.CloseElement();
        }
        else
        {
            if (ChildContent != null)
            {
                builder.AddContent(seq++, ChildContent);
            }
        }
    }

}