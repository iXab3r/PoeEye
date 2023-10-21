using Microsoft.AspNetCore.Components;
using PoeShared.Blazor.Services;
using ReactiveUI;
using PoeShared.Scaffolding;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PropertyBinder;
using Unity;

namespace PoeShared.Blazor;

partial class BlazorContentPresenter
{
    private static readonly Binder<BlazorContentPresenter> Binder = new();

    [Parameter] public object Content { get; set; }

    [Parameter] public Type ViewType { get; set; }

    [Parameter] public object ViewTypeKey { get; set; }
    
    [Inject] public IBlazorViewRepository ViewRepository { get; set; }

    public Type ResolvedViewType { get; [UsedImplicitly] private set; }
    
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Instance of resolved object, populated using ReferenceCapture in default flow
    /// </summary>
    private object View { get; set; }
    
    private RenderFragment DynamicView => builder =>
    {
        var seq = -1;

        var viewType = ResolvedViewType;
        if (viewType == null)
        {
            return;
        }

        builder.OpenComponent(seq++, viewType);
        builder.AddComponentReferenceCapture(seq++, SetView);
        builder.CloseComponent();
    };

    static BlazorContentPresenter()
    {
        Binder
            .BindIf(x => x.ViewType != null, x => x.ViewType)
            .ElseIf(x => x.ViewRepository != null && x.Content != null, x => x.ViewRepository.ResolveViewType(x.Content.GetType(), x.ViewTypeKey))
            .Else(x => null)
            .To((x, v) =>
            {
                x.ResolvedViewType = v;
                var currentView = x.View;
                if (currentView == null || currentView.GetType() == v)
                {
                    return;
                }

                // active View/ResolvedViewType mismatch - force re-render
                x.View = null;
                x.StateHasChanged();
            });
        
        Binder
            .BindIf(x => x.View is BlazorReactiveComponent, x => x.Content)
            .To((x, v) =>
            {
#pragma warning disable BL0005 this is a special case
                ((BlazorReactiveComponent) x.View).DataContext = v;
#pragma warning restore BL0005
            });
    }

    public BlazorContentPresenter()
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
        Initialize();
    }

    internal void SetView(object value)
    {
        View = value;
    }

    internal void Initialize()
    {
        IsInitialized = true;
        Binder.Attach(this).AddTo(Anchors);
    }
}