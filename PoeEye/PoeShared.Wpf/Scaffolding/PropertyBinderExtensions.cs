using System;
using System.Reactive.Concurrency;
using System.Windows.Threading;
using PropertyBinder;

namespace PoeShared.Scaffolding;

public static class PropertyBinderExtensions
{

    public static void To<T, TContext>(this IConditionalRuleBuilderPhase2<T, TContext> builder, Action<TContext, T> action, Func<TContext, IScheduler> schedulerSelector)
        where TContext : class
    {
        builder.To((x, v) =>
        {
            var scheduler = schedulerSelector(x);
            if (scheduler == null)
            {
                throw new ArgumentException($"Failed to get {typeof(IScheduler)} from context {x}");
            }
            scheduler.Schedule(() => action(x, v));
        });
    }
    
    public static void To<T, TContext>(this PropertyRuleBuilder<T, TContext> builder, Action<TContext, T> action, Func<TContext, Dispatcher> dispatcherSelector) where TContext : class
    {
        var builderClosure = builder;
        builder.To((x, v) =>
        {
            var scheduler = dispatcherSelector(x);
            if (scheduler == null)
            {
                throw new ArgumentException($"Failed to get {typeof(IScheduler)} from context {x}, builder: {builderClosure}");
            }
            scheduler.Invoke(() => action(x, v));
        });
    }

    public static void To<T, TContext>(this PropertyRuleBuilder<T, TContext> builder, Action<TContext, T> action, Func<TContext, IScheduler> schedulerSelector) where TContext : class
    {
        var builderClosure = builder;
        builder.To((x, v) =>
        {
            var scheduler = schedulerSelector(x);
            if (scheduler == null)
            {
                throw new ArgumentException($"Failed to get {typeof(IScheduler)} from context {x}, builder: {builderClosure}");
            }
            scheduler.Schedule(() => action(x, v));
        });
    }
}