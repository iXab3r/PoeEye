using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Bindings;

public abstract class ExpressionWatcherBase : DisposableReactiveObject, IValueWatcher
{
    private static readonly MethodInfo WatcherFactoryMethod = typeof(ExpressionWatcherBase).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == nameof(PrepareWatcher) && x.IsGenericMethod)
                                                              ?? throw new MissingMethodException($"Failed to find method {nameof(PrepareWatcher)} in type {typeof(ExpressionWatcherBase)}");

    private static readonly ConcurrentDictionary<(Type, Type), Func<string ,string, IValueWatcher>> TypedWatcherFactory = new();

    protected static readonly Binder<ExpressionWatcherBase> Binder = new();

    private static readonly PassthroughLinkCustomTypeProvider CustomTypeProvider = new PassthroughLinkCustomTypeProvider();

    static ExpressionWatcherBase()
    {
        Binder.Bind(x => x.Source != null ? x.Source.GetType() : default).To(x => x.SourceType);
        Binder.Bind(x => PrepareWatcher(x, x.SourceType, x.PropertyType, x.SourceExpression, x.ConditionExpression)).To(x => x.Watcher);

        Binder
            .BindIf(x => x.Watcher != null && x.Watcher.HasValue, x => x.Watcher.Value)
            .Else(x => null)
            .To(x => x.Value);
            
        Binder
            .BindIf(x => x.Watcher != null, x => x.Watcher.Error)
            .To(x => x.Error);
            
        Binder.BindIf(x => x .Watcher != null && x.Watcher.HasValue, x => true)
            .Else(x => false)
            .To(x => x.HasValue);
            
        Binder.BindIf(x => x .Watcher != null && x.Watcher.HasValue && x.Watcher.CanSetValue, x => true)
            .Else(x => false)
            .To(x => x.CanSetValue);
            
        Binder.BindIf(x => x .Watcher != null && x.Watcher.SupportsSetValue, x => true)
            .Else(x => false)
            .To(x => x.SupportsSetValue);
            
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
    }

    private IFluentLog Log { get; }

    protected string SourceExpression { get; set; }

    protected string ConditionExpression { get; set; } = "x != null";

    public Type PropertyType { get; protected set; }

    public Type SourceType { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    public IValueWatcher Watcher { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    public object Source { get; set; }

    public object Value { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    public bool CanSetValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }
    public bool SupportsSetValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    public bool HasValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    public Exception Error { get; private set; }

    public void SetCurrentValue(object newValue)
    {
        if (Watcher == null)
        {
            throw new InvalidOperationException("Watcher is not initialized");
        }
        Watcher.SetCurrentValue(newValue);
    }

    private static IValueWatcher PrepareWatcher(ExpressionWatcherBase instance, Type sourceType, Type propertyType, string sourceExprText, string conditionExprText)
    {
        if (sourceType == null || string.IsNullOrEmpty(sourceExprText) || string.IsNullOrEmpty(conditionExprText)  || propertyType == null)
        {
            return default;
        }

        try
        {
            var watcherFactory = TypedWatcherFactory.GetOrAdd((sourceType, propertyType), x => PrepareFactoryFunc(x.Item1, x.Item2));
            return watcherFactory(sourceExprText, conditionExprText);
        }
        catch (Exception e)
        {
            var ex = new BindingException($"Failed to prepare watcher, sourceType: {sourceType}, propertyType: {propertyType}, sourceExpr: {sourceExprText}, conditionExpr: {conditionExprText} - {e}", e);
            instance.Log.Warn($"Exception occured when tried to prepare watcher", ex);
            instance.Error = ex;
            return default;
        }
    }

    private static ExpressionWatcher<TSource, TProperty> PrepareWatcher<TSource, TProperty>(string sourceExprText, string conditionExprText) where TSource : class
    {
        var sourceParameter = Expression.Parameter(typeof(TSource), "x");
        var config = new ParsingConfig
        {
            ResolveTypesBySimpleName = true,
            CustomTypeProvider = CustomTypeProvider
        };

        var sourceBinderExpr = (Expression<Func<TSource, TProperty>>) DynamicExpressionParser.ParseLambda(config, false, new[] { sourceParameter }, typeof (TProperty), sourceExprText);
        var conditionBinderExpr = (Expression<Func<TSource, bool>>) DynamicExpressionParser.ParseLambda(config, false, new[] { sourceParameter }, typeof (bool), conditionExprText);

        return new ExpressionWatcher<TSource, TProperty>(sourceBinderExpr, conditionBinderExpr);
    }

    private static Func<string ,string, IValueWatcher> PrepareFactoryFunc(Type sourceType, Type propertyType)
    {
        var sourceExprParameter = Expression.Parameter(typeof(string), "sourceExprText");
        var conditionExprParameter = Expression.Parameter(typeof(string), "conditionExprText");

        var method = WatcherFactoryMethod.MakeGenericMethod(sourceType, propertyType);
        var methodExpr = Expression.Call(method, sourceExprParameter, conditionExprParameter);

        var lambda = Expression.Lambda<Func<string ,string, IValueWatcher>>(methodExpr, sourceExprParameter, conditionExprParameter);
        return PropertyBinder.Binder.ExpressionCompiler.Compile(lambda);
    }

    private sealed class PassthroughLinkCustomTypeProvider : IDynamicLinkCustomTypeProvider
    {
        private readonly IDynamicLinkCustomTypeProvider fallback;

        public PassthroughLinkCustomTypeProvider()
        {
            fallback = new DynamicLinqCustomTypeProvider();
        }

        public HashSet<Type> GetCustomTypes()
        {
            return fallback.GetCustomTypes();
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