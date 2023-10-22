using System;
using System.Linq.Expressions;
using System.Reactive.Linq;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponent : BlazorReactiveComponentBase
{
}

public abstract class BlazorReactiveComponent<TContext> : BlazorReactiveComponent where TContext : class
{
    private static readonly Binder<BlazorReactiveComponent<TContext>> Binder = new();

    static BlazorReactiveComponent()
    {
    }

    public new TContext DataContext
    {
        get => (TContext) base.DataContext;
        set => base.DataContext = value;
    }
    
    protected BlazorReactiveComponent()
    {
        this.WhenAnyValue(x => x.DataContext)
            .Select(x => x is IRefreshableComponent refreshableComponent ? refreshableComponent.WhenRefresh : Observable.Empty<object>())
            .Switch()
            .Do(_ => { })
            .Subscribe(WhenRefresh)
            .AddTo(Anchors);
        
        Binder.Attach(this).AddTo(Anchors);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    public TOut Track<TOut>(Expression<Func<TContext, TOut>> selector)
    {
        return Track(DataContext, selector);
    }
}