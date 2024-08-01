using System;
using System.Reactive.Concurrency;
using PoeShared.Modularity;
using PropertyBinder;

namespace PoeShared.Scaffolding;

public static class PropertyRuleBuilderExtensions
{
    public static PropertyRuleBuilder<T, TContext> OnScheduler<T,TContext>(this PropertyRuleBuilder<T, TContext> instance, Func<TContext, IScheduler> schedulerSupplier) where TContext : class
    {
        instance.WithAssignmentAction((context, action) => RescheduleIfNeeded(context, action, schedulerSupplier));
        return instance;
    }
    
    public static Binder<TContext> OnScheduler<TContext>(this Binder<TContext> instance, Func<TContext, IScheduler> schedulerSupplier) where TContext : class
    {
        instance.WithAssignmentAction((context, action) => RescheduleIfNeeded(context, action, schedulerSupplier));
        return instance;
    }

    private static void RescheduleIfNeeded<TContext>(TContext context, Action<TContext> action, Func<TContext, IScheduler> schedulerSupplier)
    {
        var scheduler = schedulerSupplier(context);
        if (scheduler == null)
        {
            throw new InvalidOperationException($"Failed to resolve scheduler from context {context}");
        }

        if (scheduler is not DispatcherScheduler dispatcherScheduler)
        {
            throw new InvalidOperationException($"It is expected that scheduler will be of type {typeof(DispatcherScheduler)}, but was {scheduler.GetType()}");
        }

        if (dispatcherScheduler.CheckAccess())
        {
            action(context);
        }
        else
        {
            try
            {
                dispatcherScheduler.Dispatcher.Invoke(() => action(context));
            }
            catch (OperationCanceledException)
            {
                //if dispatcher is being shutdown, Invoke will throw
                //considering binding is hard-wired to the scheduler, it makes sense to swallow this
            }
        }
    }
}