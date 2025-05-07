using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    
    public Type ViewType { get; set; }

    public object ViewTypeKey { get; set; }

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
        Log.Debug("ContentPresenter is being created");
        Disposable.Create(() => View = null).AddTo(Anchors);
        
        this.WhenAnyProperty(x => x.Content)
            .Select(x => Content)
            .WithPrevious()
            .Skip(1)
            .Subscribe(x =>
            {
                Log.Debug($"ContentPresenter view has been updated: {x}");
                if (x.Previous is IDisposable disposableView)
                {
                    disposableView.Dispose();
                }
            })
            .AddTo(Anchors);
    }

    protected override void OnInitialized()
    {
        Log.Debug($"ContentPresenter is being initialized, view: {View}");

        base.OnInitialized();
        this.WhenAnyProperty(x => x.Content)
            .SubscribeAsync(x => Refresh($"Content has been updated"))
            .AddTo(Anchors);
        this.WhenAnyProperty(x => x.ViewType)
            .SubscribeAsync(x => Refresh($"ViewType has been updated"))
            .AddTo(Anchors);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();
        
        Log.Debug($"ContentPresenter has been rendered for the first time, view: {View}");
    }
}