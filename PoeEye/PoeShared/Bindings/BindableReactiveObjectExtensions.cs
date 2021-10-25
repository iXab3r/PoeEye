using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public static class BindableReactiveObjectExtensions
    {
        private static readonly IFluentLog Log = typeof(BindableReactiveObjectExtensions).PrepareLogger();

        public static IDisposable AddOrUpdateBinding<TTarget, TSource, TProperty>(this TTarget instance, Expression<Func<TTarget, TProperty>> targetProperty, TSource source, Expression<Func<TSource, TProperty>> sourceExpression) 
            where TTarget : BindableReactiveObject
        where TSource : DisposableReactiveObject
        {
            var sourceWatcher = new ExpressionWatcher<TSource, TProperty>(sourceExpression);
            sourceWatcher.Source = source;
            var targetWatcher = new ExpressionWatcher<TTarget, TProperty>(targetProperty);
            targetWatcher.Source = instance;
            var newBinding = new ReactiveBinding(sourceWatcher, targetWatcher);
            return instance.AddOrUpdateBinding(newBinding);
        }
    }
}