using System;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Blazor.Internals;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Scaffolding;

public static class ReactiveObjectExtensions
{
    public static IObservable<string> Listen<TContext, TOut>(this TContext context, Expression<Func<TContext, TOut>> selector) where TContext : class
    {
        return Observable.Create<string>(observer =>
        {
            var anchors = new CompositeDisposable();

            var detector = new ChangeTracker<TContext, TOut>(context, selector).AddTo(anchors);
            detector.WhenChanged.Select(x => x.ToString()).Subscribe(observer).AddTo(anchors);
            anchors.Add(() => { });
            return anchors;
        });
    }
    
    public static IObservable<string> Listen<TContext, TOut1, TOut2>(
        this TContext context, 
        Expression<Func<TContext, TOut1>> selector1, 
        Expression<Func<TContext, TOut2>> selector2) where TContext : class
    {
        return Observable.Merge(
            Listen(context, selector1),
            Listen(context, selector2));
    }
    
    public static IObservable<string> Listen<TContext, TOut1, TOut2, TOut3>(
        this TContext context, 
        Expression<Func<TContext, TOut1>> selector1, 
        Expression<Func<TContext, TOut2>> selector2,
        Expression<Func<TContext, TOut3>> selector3) 
        where TContext : class
    {
        return Observable.Merge(
            Listen(context, selector1),
            Listen(context, selector2),
            Listen(context, selector3));
    }
    
    public static IObservable<string> Listen<TContext, TOut1, TOut2, TOut3, TOut4>(
        this TContext context, 
        Expression<Func<TContext, TOut1>> selector1, 
        Expression<Func<TContext, TOut2>> selector2,
        Expression<Func<TContext, TOut3>> selector3,
        Expression<Func<TContext, TOut4>> selector4) 
        where TContext : class
    {
        return Observable.Merge(
            Listen(context, selector1),
            Listen(context, selector2),
            Listen(context, selector3),
            Listen(context, selector4));
    }
}