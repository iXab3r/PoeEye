using System;
using System.Collections.Generic;
using System.Reactive.Linq;
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
            /*
             There is a potential for performance improvement - current system re-renders tracker changes right after the first render
             which is a waste of resources. 
             Brute-force approach to just skip events until render wont work as in many cases subscription is happening 
             inside OnAfterFirstRenderAsync, meaning any changes done there would be lost. 
             Brute-brute-force way of solving this would be to just always render the component at least twice.
             
             Need to put some thought into improving design of this
             
             This is a bad idea:
                //skip all request before until we've been rendered at least once
                .Where(x => IsComponentRendered) 
                //in many cases there is some logic in OnAfterFirstRenderAsync
                .StartWith("ReactiveSection - re-render to handle first-render changes") 
            */
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

        const int seq = 0;

        var hasAttribute = !string.IsNullOrEmpty(Id) ||
                           !string.IsNullOrEmpty(Class) ||
                           !string.IsNullOrEmpty(Style) ||
                           AdditionalAttributes != null;
        if (AsElement != null || hasAttribute)
        {
            builder.OpenElement(seq + 1, AsElement ?? "span");
        
            if (!string.IsNullOrEmpty(Id))
            {
                builder.AddAttribute(seq + 2, "id", Id);
            }
        
            if (!string.IsNullOrEmpty(Class))
            {
                builder.AddAttribute(seq + 3, "class", Class);
            }
        
            if (!string.IsNullOrEmpty(Style))
            {
                builder.AddAttribute(seq + 4, "style", Style);
            }
        
            if (AdditionalAttributes != null)
            {
                foreach (var attribute in AdditionalAttributes)
                {
                    builder.AddAttribute(seq + 5, attribute.Key, attribute.Value);
                }
            }
        
            var childContent = ChildContent;
            if (childContent != null)
            {
                builder.AddContent(seq + 6, childContent);
            }
            builder.CloseElement();
        }
        else
        {
            var childContent = ChildContent;
            if (childContent != null)
            {
                builder.AddContent(seq + 100, childContent);
            }
        }
    }

}