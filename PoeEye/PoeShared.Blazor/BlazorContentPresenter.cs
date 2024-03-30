using Microsoft.AspNetCore.Components;
using PoeShared.Blazor.Services;
using ReactiveUI;
using PoeShared.Scaffolding;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    
    /// <summary>
    /// Instance of resolved object, populated using ReferenceCapture in default flow
    /// </summary>
    private object View { get; set; }
    
    private RenderFragment DynamicView => builder =>
    {
        var viewType = ResolvedViewType;
        if (viewType == null)
        {
            return;
        }

        builder.OpenComponent(0, viewType);
        if (viewType.IsAssignableTo(typeof(BlazorReactiveComponent)))
        {
            builder.AddAttribute(1, nameof(BlazorReactiveComponent.DataContext), Content);
        }
        builder.AddComponentReferenceCapture(2, SetView);
        builder.CloseComponent();
    };

    static BlazorContentPresenter()
    {
        Binder
            .BindIf(x => x.ViewType != null, x => x.ViewType)
            .ElseIf(x => x.ViewRepository != null && x.Content != null, x => new ViewResolver(x.ViewRepository, x.Content.GetType(), x.ViewTypeKey).ViewType)
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

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
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
        this.WhenAnyValue(x => x.Content)
            .Skip(1)
            .SubscribeAsync(x => Refresh($"Content has been updated to {x}"))
            .AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
    }

    private sealed class ViewResolver : DisposableReactiveObject
    {
        public ViewResolver(IBlazorViewRepository viewRepository, Type contentType, object viewTypeKey)
        {
            ViewRepository = viewRepository;
            ContentType = contentType;
            ViewTypeKey = viewTypeKey;
            Resolve();
            //FIXME Implement delayed template initialization, i.e. support reload if view is registered AFTER display
        }
        
        public IBlazorViewRepository ViewRepository { get; }
        
        public Type ContentType { get; }
        
        public object ViewTypeKey { get; }
        
        public Type ViewType { get; private set; }

        private void Resolve()
        {
            ViewType = ViewRepository.ResolveViewType(ContentType, key: ViewTypeKey);
        }
    }
}