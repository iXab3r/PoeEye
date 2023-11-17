using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;
using PropertyBinder;

namespace PoeShared.Bindings;

/// <summary>
/// Serves as the base class for all watchers in the binding mechanism. It initializes and manages the binding and watching process.
/// </summary>
public abstract class ExpressionWatcherBase : DisposableReactiveObject, IValueWatcher
{
    private static readonly MethodInfo WatcherFactoryMethod = typeof(ExpressionWatcherBase).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == nameof(PrepareWatcher) && x.IsGenericMethod)
                                                              ?? throw new MissingMethodException($"Failed to find method {nameof(PrepareWatcher)} in type {typeof(ExpressionWatcherBase)}");

    private static readonly ConcurrentDictionary<(Type, Type), Func<string, string, IValueWatcher>> TypedWatcherFactory = new();

    protected static readonly Binder<ExpressionWatcherBase> Binder = new();


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

        Binder.BindIf(x => x.Watcher != null && x.Watcher.HasValue, x => true)
            .Else(x => false)
            .To(x => x.HasValue);

        Binder.BindIf(x => x.Watcher != null && x.Watcher.HasValue && x.Watcher.CanSetValue, x => true)
            .Else(x => false)
            .To(x => x.CanSetValue);

        Binder.BindIf(x => x.Watcher != null && x.Watcher.SupportsSetValue, x => true)
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

    /// <summary>
    /// Gets or sets the type of the property being watched.
    /// </summary>
    public Type PropertyType { get; protected set; }

    /// <summary>
    /// Gets the type of the source from which the value is being watched.
    /// </summary>
    public Type SourceType { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Gets the underlying value watcher.
    /// </summary>
    public IValueWatcher Watcher { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Gets or sets the source object for the watcher.
    /// </summary>
    public object Source { get; set; }

    /// <summary>
    /// Gets the current value being watched.
    /// </summary>
    public object Value { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Indicates whether the current value can be set.
    /// </summary>
    public bool CanSetValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Indicates whether setting a value is supported.
    /// </summary>
    public bool SupportsSetValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Indicates whether a value is currently available.
    /// </summary>
    public bool HasValue { get; [JetBrains.Annotations.UsedImplicitly] private set; }

    /// <summary>
    /// Gets the error that occurred during the watching process.
    /// </summary>
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
        if (sourceType == null || string.IsNullOrEmpty(sourceExprText) || string.IsNullOrEmpty(conditionExprText) || propertyType == null)
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
        var sourceBinderExpr = CsharpExpressionParser.Instance.ParseFunction<TSource, TProperty>($"{sourceExprText}");
        var conditionBinderExpr = CsharpExpressionParser.Instance.ParseFunction<TSource, bool>($"{conditionExprText}");

        return new ExpressionWatcher<TSource, TProperty>(sourceBinderExpr, conditionBinderExpr);
    }

    private static Func<string, string, IValueWatcher> PrepareFactoryFunc(Type sourceType, Type propertyType)
    {
        var sourceExprParameter = Expression.Parameter(typeof(string), "sourceExprText");
        var conditionExprParameter = Expression.Parameter(typeof(string), "conditionExprText");

        var method = WatcherFactoryMethod.MakeGenericMethod(sourceType, propertyType);
        var methodExpr = Expression.Call(method, sourceExprParameter, conditionExprParameter);

        var lambda = Expression.Lambda<Func<string, string, IValueWatcher>>(methodExpr, sourceExprParameter, conditionExprParameter);
        return PropertyBinder.Binder.ExpressionCompiler.Compile(lambda);
    }
}