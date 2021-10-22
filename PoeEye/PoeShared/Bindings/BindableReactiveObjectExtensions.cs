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
        
        public static IDisposable AddOrUpdateBinding<TTarget>(this TTarget instance, IObservable<object> source,
            Type sourceType,
            string sourcePath,
            string targetPath)
            where TTarget : IBindableReactiveObject
        {
            instance.RemoveBinding(targetPath);
            var binding = CreateBinding(instance, source, sourcePath, targetPath);
            return instance.AddOrUpdateBinding(binding);
        }
        
        public static IReactiveBinding CreateBinding<TTarget>(
            this TTarget target, 
            IObservable<object> source,
            string sourcePath,
            string targetPath)
            where TTarget : IBindableReactiveObject
        {
            // source is dynamic - it could be loaded/unloaded/changed in realtime
            var sourceWatcher = new PropertyPathWatcher()
            {
                PropertyPath = @"Value." + sourcePath,
            };
            var dynamicSource = new ObservableValueWrapper(source, sourceWatcher).AddTo(sourceWatcher.Anchors);
            // target is static - if it's loaded then binding is disabled entirely
            var targetWatcher = new PropertyPathWatcher
            {
                PropertyPath = targetPath,
                Source = target
            };

            return new ReactiveBinding(sourceWatcher, targetWatcher, targetPath);
        }
    }
}