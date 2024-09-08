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
        var refreshRequestSource = this.WhenAnyValue(x => x.Trackers)
            .Select(x => x ?? new ReactiveTrackerList())
            .Select(x => x.Merge())
            .Switch();
        
        this.WhenAnyValue(x => x.DebounceTime)
            .Select(x => x <= TimeSpan.Zero ? refreshRequestSource : refreshRequestSource.Sample(x))
            .Switch()
            .SubscribeAsync(x => Refresh(x))
            //.Subscribe(x => WhenRefresh.OnNext(x)) //FIXME More valid way to do refreshes, but extremely slows down everything - should be investigated
            .AddTo(Anchors);
    }
    
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; }
    
    [Parameter] public ReactiveTrackerList Trackers { get; set; }

    [Parameter] public string AsElement { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)] public IEnumerable<KeyValuePair<string, object>> AdditionalAttributes { get; set; }
    
    [Parameter] public TimeSpan DebounceTime { get; set; } = TimeSpan.Zero;
    
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
        
            var childContent = ChildContent;
            if (childContent != null)
            {
                builder.AddContent(seq++, childContent);
            }
            builder.CloseElement();
        }
        else
        {
            var childContent = ChildContent;
            if (childContent != null)
            {
                builder.AddContent(seq++, childContent);
            }
        }
    }

}