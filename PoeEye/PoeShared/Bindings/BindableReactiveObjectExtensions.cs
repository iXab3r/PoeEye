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

        private static readonly MethodInfo CreateWatcherMethod = typeof(BindableReactiveObjectExtensions).GetMethod(nameof(BindableReactiveObjectExtensions.CreateTypedWatcher), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly ConcurrentDictionary<Type, Func<IObservable<object>, object>> TypedCreateWatcherFactoryByType = new();

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
            var binding = CreateBinding(instance, source, sourceType, sourcePath, targetPath);
            return instance.AddOrUpdateBinding(binding);
        }
        
        public static IReactiveBinding CreateBinding<TTarget>(
            this TTarget target, 
            IObservable<object> source,
            Type sourceType,
            string sourcePath,
            string targetPath)
            where TTarget : IBindableReactiveObject
        {
            // source is dynamic - it could be loaded/unloaded/changed in realtime
            var dynamicSource = TypedCreateWatcherFactoryByType.GetOrAdd(sourceType, PrepareFactoryFunc).Invoke(source);
            var propertyType = sourceType.GetPropertyInfo(sourcePath);
            var sourceWatcher = new ExpressionWatcher(propertyType.PropertyType)
            {
                SourceExpression = @"x.Value." + sourcePath,
                ConditionExpression = @"x != null && x.Value != null",
                Source = dynamicSource
            };
            
            // target is static - if it's loaded then binding is disabled entirely
            var targetWatcher = new PropertyPathWatcher
            {
                PropertyPath = targetPath,
                Source = target
            };

            return new ReactiveBinding(sourceWatcher, targetWatcher, targetPath);
        }

        private static Func<IObservable<object>, object> PrepareFactoryFunc(Type valueType)
        {
            var valueSourceParameter = Expression.Parameter(typeof(IObservable<object>), "valueSource");

            var method = CreateWatcherMethod.MakeGenericMethod(valueType);
            var methodExpr = Expression.Call(method, valueSourceParameter);

            var lambda = Expression.Lambda<Func<IObservable<object>, object>>(methodExpr, valueSourceParameter);
            return PropertyBinder.Binder.ExpressionCompiler.Compile(lambda);
        }

        private static ObservableValueWatcher<T> CreateTypedWatcher<T>(IObservable<object> valueSource)
        {
            var stream = valueSource.Select(x =>
            {
                if (x == default)
                {
                    return default(T);
                }

                if (x is T arg)
                {
                    return arg;
                }

                throw new InvalidOperationException($"Provided value must be of type {typeof(T)} but was {x.GetType()}, value: {x}");
            });
            return new ObservableValueWatcher<T>(stream);
        }
    }
}