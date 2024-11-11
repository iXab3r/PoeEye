using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// This class wraps BlazorContentPresenter and is needed only inside WPF-infrastructure
/// Main reason why it is not a "normal" Razor component is to make it internal, which is not supported as of October 2023
/// </summary>
internal sealed class BlazorContentPresenterWrapper : ReactiveComponentBase
{
    public object Content { get; set; }
    
    public object ViewTypeKey { get; init; }
    
    public Type ViewType { get; init; }

    public object View { get; private set; }
    
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        builder.OpenComponent<BlazorContentPresenter>(0);
        builder.AddAttribute(1, "Content", Content);
        builder.AddAttribute(2, "ViewTypeKey", ViewTypeKey);
        builder.AddAttribute(3, "ViewType", ViewType);
        builder.AddComponentReferenceCapture(4, view => View = view);
        builder.CloseComponent();
    }

    public BlazorContentPresenterWrapper()
    {
        Disposable.Create(() => View = null).AddTo(Anchors);
        
        this.WhenAnyValue(x => x.View)
            .WithPrevious()
            .Subscribe(x =>
            {
                if (x.Previous is IDisposable disposableView)
                {
                    disposableView.Dispose();
                }
            })
            .AddTo(Anchors);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.WhenAnyProperty(x => x.Content)
            .SubscribeAsync(x => Refresh($"Content has been updated"))
            .AddTo(Anchors);
    }
}