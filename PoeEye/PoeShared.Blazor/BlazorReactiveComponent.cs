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
        get
        {
            var baseContext = base.DataContext;
            if (baseContext == null)
            {
                return default;
            }

            if (baseContext is not TContext context)
            {
                throw new InvalidOperationException($"Component {GetType()} supports contexts of type {typeof(TContext)}, but assigned type {baseContext.GetType()}, value: {base.DataContext}");
            }

            return context;
        }
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

    public void TrackState<TExpressionContext, TOut>(TExpressionContext context, Expression<Func<TExpressionContext, TOut>> selector) where TExpressionContext : class
    {
        Track(context, selector);
    }
    
    public void TrackState<TOut>(Expression<Func<TContext, TOut>> selector)
    {
        Track(selector);
    }

    public TOut Track<TOut>(Expression<Func<TContext, TOut>> selector)
    {
        return Track(DataContext, selector);
    }
}