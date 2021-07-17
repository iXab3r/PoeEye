using System;
using System.Reactive.Concurrency;
using PropertyBinder;

namespace PoeShared.Scaffolding
{
    public static class PropertyBinderExtensions
    {
        public static void To<T, TContext>(this PropertyRuleBuilder<T, TContext> builder, Action<TContext, T> action, Func<TContext, IScheduler> schedulerSelector) where TContext : class
        {
            builder.To((x, v) =>
            {
                var scheduler = schedulerSelector(x);
                scheduler.Schedule(() => action(x, v));
            });
        } 
    }
}