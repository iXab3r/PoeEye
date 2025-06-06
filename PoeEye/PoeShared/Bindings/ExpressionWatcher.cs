using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Bindings;

public class ExpressionWatcher : ExpressionWatcherBase
{
    private new static readonly Binder<ExpressionWatcher> Binder;

    static ExpressionWatcher()
    {
        Binder = ExpressionWatcherBase.Binder.Clone<ExpressionWatcher>();
        Binder.Bind(x => x.SourceExpression).To(x => x.SourceExpression);
        Binder.Bind(x => x.ConditionExpression).To(x => x.ConditionExpression);
    }

    public ExpressionWatcher(Type propertyType)
    {
        PropertyType = propertyType;
        Binder.Attach(this).AddTo(Anchors);
    }

    public new string SourceExpression
    {
        get => base.SourceExpression;
        set => base.SourceExpression = value;
    }

    public new string ConditionExpression
    {
        get => base.ConditionExpression;
        set => base.ConditionExpression = value;
    }
    
    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append($"EW: {SourceExpression}");
        builder.AppendParameter(nameof(ConditionExpression), ConditionExpression);
    }
}

public sealed class ExpressionWatcher<TSource, TProperty> : DisposableReactiveObject, IValueWatcher where TSource : class
{
    private static readonly Binder<ExpressionWatcher<TSource, TProperty>> Binder = new();

    private readonly Lazy<Action<TSource, TProperty>> assignmentActionSupplier;
    private readonly Expression<Func<TSource, bool>> condition;

    private readonly Expression<Func<TSource, TProperty>> sourceAccessor;
    private readonly Binder<TSource> sourceBinder;
    private readonly SerialDisposable sourceBinderAnchors;

    private readonly string watcherId = $"EW#{Interlocked.Increment(ref ExpressionWatcherHelper.GlobalIdx)}";
    
    private static readonly ConcurrentDictionary<(Type, Type, Expression<Func<TSource, TProperty>>, Expression<Func<TSource, bool>>), object> BindersByType = new();

    static ExpressionWatcher()
    {
        Binder.Bind(x => x.SupportsSetValue && x.Source != null).To(x => x.CanSetValue);
    }

    public ExpressionWatcher(
        Expression<Func<TSource, TProperty>> sourceAccessor, 
        Expression<Func<TSource, bool>> condition)
    {
        Log = typeof(ExpressionWatcher<TSource, TProperty>).PrepareLogger("ExpressionWatcher").WithSuffix(watcherId).WithSuffix(ToString);
        Log.Debug($"Expression is being created, source: {sourceAccessor}, condition: {condition}");

        this.sourceAccessor = sourceAccessor;
        this.condition = condition;
        sourceBinderAnchors = new SerialDisposable().AddTo(Anchors);
        SourceExpression = sourceAccessor.ToString();
            
        sourceBinder = new Binder<TSource>().WithExceptionHandler(HandleBinderException);
        sourceBinder
            .BindIf(condition, sourceAccessor)
            .Else(x => default)
            .To((x, v) =>
            {
                Error = default;
                Value = v;
            });
            
        sourceBinder
            .BindIf(condition, x => true)
            .Else(x => false)
            .To((x, v) => HasValue = v);
        Log.Debug($"Expression has prepared the binder, source: {sourceAccessor}, condition: {condition}");

        SupportsSetValue = CanPrepareSetter(sourceAccessor);
        assignmentActionSupplier = new Lazy<Action<TSource, TProperty>>(() => SupportsSetValue ? PrepareSetter(sourceAccessor) : throw new NotSupportedException($"Assignment is not supported for {sourceAccessor}"));
            
        this.WhenAnyValue(x => x.Source)
            .WithPrevious()
            .SubscribeSafe(x =>
            {
                if (sourceBinderAnchors.Disposable != null && x.Previous != null)
                {
                    Log.Debug($"Unbinding from existing source {x.Previous}");
                    sourceBinderAnchors.Disposable = default;
                }

                if (x.Current != null)
                {
                    Log.Debug($"Binding to source {x.Current}");
                }
                Error = default;
                sourceBinderAnchors.Disposable = sourceBinder.Attach(x.Current);
            }, Log.HandleUiException)
            .AddTo(Anchors);

        Disposable.Create(() =>
        {
            Log.Debug("Instance disposed, resetting all values");
            CanSetValue = false;
            Value = default;
            HasValue = false;
            Source = default;
        }).AddTo(Anchors);
            
        Log.Debug($"Attaching binder, source: {sourceAccessor}, condition: {condition}");
        Binder.Attach(this).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
        
        Log.Debug($"Expression has being created, source: {sourceAccessor}, condition: {condition}");
    }

    public ExpressionWatcher(Expression<Func<TSource, TProperty>> sourceAccessor) : this(sourceAccessor, x => x != null)
    {
    }

    private IFluentLog Log { get; }

    public string SourceExpression { get; }

    public TProperty Value { get; private set; }

    public TSource Source { get; set; }

    /// <inheritdoc />
    public Exception Error { get; private set; }

    /// <inheritdoc />
    public bool HasValue { get; private set; }

    /// <inheritdoc />
    public bool CanSetValue { get; private set; }

    /// <inheritdoc />
    public bool SupportsSetValue { get; }

    /// <inheritdoc />
    object IValueProvider.Value => Value;

    /// <inheritdoc />
    object IValueWatcher.Source
    {
        get => Source;
        set
        {
            if (value is TSource or null)
            {
                Source = (TSource)value;
            }
            else
            {
                Source = default;
                var error = new BindingException($"Failed to set source - value is of type {value.GetType()}, expected {typeof(TSource)}");
                Log.Warn($"Could not set source of watcher {this}", error);
                Error = error;
            }
        }
    }

    public void SetCurrentValue(object newValue)
    {
        if (!TryConvertToPropertyType(newValue, out var converted))
        {
            var error = new BindingException($"Failed to set current value of {Source} from {Value} to {newValue} - failed to convert type, expected {typeof(TProperty)}, got {newValue.GetType()}");
            Log.Warn($"Could not set current value of watcher {this}", error);
            Error = error;
            return;
        }
        
        if (converted == null && default(TProperty) != null)
        {
            Log.Warn($"Target Property is of type {typeof(TProperty)} which is non-nullable, ignoring SetCurrentValue({newValue}) request");
        }
        else
        {
            var typed = (TProperty)converted;
            SetCurrentValue(typed);
        }
    }

    private bool TryConvertToPropertyType(object value, out object result)
    {
        switch (value)
        {
            case null:
                result = null;
                break;
            case TProperty:
                result = value;
                break;
            default:
            {
                if (typeof(TProperty) == typeof(string))
                {
                    result = value.ToString();
                }
                else
                {
                    result = default;
                    return false;
                }
                break;
            }
        }
        return true;
    }

    private void HandleBinderException(object sender, BindingExceptionEventArgs e)
    {
        Error = e.Exception;
        e.Handled = true;
        Log.Warn($"Binder encountered exception: {e}", e.Exception);
    }

    public void SetCurrentValue(TProperty newValue)
    {
        if (Source == default)
        {
            throw new InvalidOperationException($"Source is not set, could not set value {newValue}");
        }

        var beforeUpdate = Value;
        Log.Debug($"Updating value: {beforeUpdate} => {newValue}");
        try
        {
            Error = default;
            assignmentActionSupplier.Value(Source, newValue);
            var afterUpdate = Value;
            if (EqualityComparer<TProperty>.Default.Equals(newValue, afterUpdate))
            {
                Log.Debug($"Updated value to {afterUpdate}, source: {Source}");
            }
            else
            {
                Log.Debug($"Failed to update value to {newValue}, currently it is {afterUpdate}, source: {Source}");
            }
        }
        catch (Exception e)
        {
            Error = new BindingException($"Failed to set value {newValue}, watcher: {this} - {e}", e);
            Log.Warn($"Failed to change value of {Source} from {beforeUpdate} to {newValue}", e);
        }
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append($"EW: {SourceExpression}");
        builder.AppendParameter(nameof(Source), Source == default ? "not set" : Source.ToString());
        builder.AppendParameter(nameof(HasValue), HasValue ? $"value: {Value}" : $"hasValue: {HasValue}");
    }

    private static bool CanPrepareSetter(Expression<Func<TSource, TProperty>> propertyAccessor)
    {
        if (propertyAccessor.Body is not MemberExpression memberExpression)
        {
            return false;
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            return false;
        }

        return propertyInfo.CanWrite;
    }

    private static Action<TSource, TProperty> PrepareSetter(Expression<Func<TSource, TProperty>> propertyAccessor)
    {
        if (!CanPrepareSetter(propertyAccessor))
        {
            throw new ArgumentException($"Unsupported expression: {propertyAccessor}");
        }
        var targetExp = propertyAccessor.Parameters[0];
        var valueExp = Expression.Parameter(typeof(TProperty), "v");

        var memberExp = (MemberExpression)propertyAccessor.Body;
        var assign = Expression.Assign(memberExp, valueExp);
        var setter = PropertyBinder.Binder.ExpressionCompiler.Compile(Expression.Lambda<Action<TSource, TProperty>>(assign, targetExp, valueExp));
        return setter;
    }
}