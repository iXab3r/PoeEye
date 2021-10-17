using System;
using System.Linq.Expressions;
using PoeShared.Scaffolding;

namespace PoeShared.Bindings
{
    public static class BindableReactiveObjectExtensions
    {
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