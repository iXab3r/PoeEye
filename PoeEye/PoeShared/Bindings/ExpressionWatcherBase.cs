using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Linq.Expressions;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings
{
    public abstract class ExpressionWatcherBase : DisposableReactiveObject, IValueWatcher
    {
        private static readonly Binder<ExpressionWatcherBase> Binder = new Binder<ExpressionWatcherBase>();

        static ExpressionWatcherBase()
        {
            Binder.Bind(x => x.Source != null ? x.Source.GetType() : default).To(x => x.SourceType);
            Binder.Bind(x => PrepareWatcher(x.SourceType, x.PropertyType, x.SourceExpression, x.ConditionExpression)).To(x => x.Watcher);

            Binder
                .BindIf(x => x.Watcher != null && x.Watcher.HasValue, x => x.Watcher.Value)
                .Else(x => null)
                .To(x => x.Value);
            
            Binder.BindIf(x => x .Watcher != null && x.Watcher.HasValue, x => true)
                .Else(x => false)
                .To(x => x.HasValue);
            
            Binder.BindIf(x => x .Watcher != null  && x.Watcher.HasValue && x.Watcher.CanSetValue, x => true)
                .Else(x => false)
                .To(x => x.CanSetValue);
            
            Binder
                .Bind(x => x.Source)
                .WithDependency(x => x.Watcher)
                .To((x, v) =>
                {
                    if (x.Watcher != null)
                    {
                        x.Watcher.Source = v;
                    }
                });
        }

        protected ExpressionWatcherBase()
        {
            Log = typeof(ExpressionWatcher).PrepareLogger();
            Binder.Attach(this).AddTo(Anchors);
        }

        private IFluentLog Log { get; }

        protected string SourceExpression { get; set; }

        protected string ConditionExpression { get; set; } = "x => x != null";

        public Type PropertyType { get; protected set; }

        public Type SourceType { get; [JetBrains.Annotations.UsedImplicitly] private set; }

        public IValueWatcher Watcher { get; [JetBrains.Annotations.UsedImplicitly] private set; }

        public object Source { get; set; }

        public object Value { get; [JetBrains.Annotations.UsedImplicitly] private set; }
        
        public bool CanSetValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

        public bool HasValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

        public void SetCurrentValue(object newValue)
        {
            if (Watcher == null)
            {
                throw new InvalidOperationException("Watcher is not initialized");
            }
            Watcher.SetCurrentValue(newValue);
        }

        private static IValueWatcher PrepareWatcher(Type sourceType, Type propertyType, string sourceExprText, string conditionExprText)
        {
            if (sourceType == null || string.IsNullOrEmpty(sourceExprText) || string.IsNullOrEmpty(conditionExprText)  || propertyType == null)
            {
                return null;
            } 
            
            var watcherFactoryMethod = typeof(ExpressionWatcherBase).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == nameof(PrepareWatcher) && x.IsGenericMethod) ?? throw new MissingMethodException($"Failed to find method {nameof(PrepareWatcher)} in type {typeof(ExpressionWatcher)}");
            var watcherFactory = watcherFactoryMethod.MakeGenericMethod(sourceType, propertyType);
            var watcher = (IValueWatcher)watcherFactory.Invoke(null, new[] { sourceExprText, conditionExprText });
            return watcher;
        }

        private static ExpressionWatcher<TSource, TProperty> PrepareWatcher<TSource, TProperty>(string sourceExprText, string conditionExprText) where TSource : class
        {
            var sourceParameter = Expression.Parameter(typeof(TSource), "x");
            var config = new ParsingConfig
            {
                ResolveTypesBySimpleName = true,
                CustomTypeProvider = new PassthroughLinkCustomTypeProvider(typeof(TSource), typeof(TProperty))
            };

            var sourceBinderExpr = (Expression<Func<TSource, TProperty>>) DynamicExpressionParser.ParseLambda(config, false, new[] { sourceParameter }, typeof (TProperty), sourceExprText);
            var conditionBinderExpr = (Expression<Func<TSource, bool>>) DynamicExpressionParser.ParseLambda(config, false, new[] { sourceParameter }, typeof (bool), conditionExprText);

            return new ExpressionWatcher<TSource, TProperty>(sourceBinderExpr, conditionBinderExpr);
        }

        private sealed class PassthroughLinkCustomTypeProvider : IDynamicLinkCustomTypeProvider
        {
            private readonly HashSet<Type> customTypes;
            private IDynamicLinkCustomTypeProvider fallback;
            
            public PassthroughLinkCustomTypeProvider(params Type[] customType)
            {
                fallback = new DynamicLinqCustomTypeProvider();
                customTypes = new HashSet<Type>(customType);
            }

            public HashSet<Type> GetCustomTypes()
            {
                return customTypes;
            }

            public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
            {
                return fallback.GetExtensionMethods();
            }

            public Type ResolveType(string typeName)
            {
                return fallback.ResolveType(typeName);
            }

            public Type ResolveTypeBySimpleName(string simpleTypeName)
            {
                return fallback.ResolveTypeBySimpleName(simpleTypeName);
            }
        }
    }
}