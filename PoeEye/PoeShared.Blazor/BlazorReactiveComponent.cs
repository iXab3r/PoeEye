using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using PoeShared.Blazor.Internals;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor;

public abstract class BlazorReactiveComponent : BlazorReactiveComponentBase
{
   
}

public abstract class BlazorReactiveComponent<TContext> : BlazorReactiveComponent where TContext : class
{
    private readonly Subject<object> whenChanged = new();
    private readonly ReactiveChangeDetector changeDetector = new();

    private static readonly Binder<BlazorReactiveComponent<TContext>> Binder = new();

    static BlazorReactiveComponent()
    {
    }

    public new TContext DataContext
    {
        get => (TContext) base.DataContext;
        set => base.DataContext = value;
    }

    public IObservable<object> WhenChanged => whenChanged;
    
    protected BlazorReactiveComponent()
    {
        var forceRefresh = this.WhenAnyValue(x => x.DataContext)
            .Select(x => x is IRefreshableComponent refreshableComponent ? refreshableComponent.WhenRefresh : Observable.Empty<object>())
            .Switch();

        Observable.Merge(
                forceRefresh,
                changeDetector.WhenChanged)
            .Subscribe(x => whenChanged.OnNext(x))
            .AddTo(Anchors);
        
        Binder.Attach(this).AddTo(Anchors);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        whenChanged.Subscribe(WhenRefresh).AddTo(Anchors);
    }

    public TOut Track<TOut>(Expression<Func<TContext, TOut>> selector)
    {
        return changeDetector.Track(DataContext, selector);
    }
    
    public TOut Track<TContext, TOut>(TContext context, Expression<Func<TContext, TOut>> selector) where TContext : class
    {
        return changeDetector.Track(context, selector);
    }
}