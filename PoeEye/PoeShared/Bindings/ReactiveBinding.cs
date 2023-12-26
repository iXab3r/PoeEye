using System.Linq.Expressions;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Bindings;

/// <summary>
/// Represents a reactive binding that establishes a connection between a source value provider and a target watcher.
/// This binding becomes active when both the source and target have valid values and is used to propagate changes 
/// from the source to the target, allowing for reactive updates in a data-driven application.
/// </summary>
public class ReactiveBinding : DisposableReactiveObject, IReactiveBinding
{
    private static readonly Binder<ReactiveBinding> Binder = new();

    static ReactiveBinding()
    {
        Binder
            .Bind(x => x.SourceWatcher.HasValue && x.TargetWatcher.HasValue)
            .To(x => x.IsActive);
            
        Binder.Bind(x => new[]
        {
            x.TargetWatcher.Error == null ? null : $"Target Watcher error: {x.TargetWatcher.Error}",
            x.SourceWatcher.Error == null ? null : $"Source Watcher error: {x.SourceWatcher.Error}"
        }.Where(y => y != null).JoinStrings(Environment.NewLine)).To((x, v) => x.Error = string.IsNullOrEmpty(v) ? default : v);
            
        Binder.BindIf(x => x.IsActive, x => x.SourceWatcher.Value)
            .ElseIf(x => x.TargetWatcher.HasValue, x => default)
            .To((x, v) =>
            {
                x.Log.Debug($"Propagating value {v ?? "NULL"}");
                x.TargetWatcher.SetCurrentValue(v);
            });
            
            
        Binder.BindIf(x => x.TargetWatcher.HasValue, x => x.TargetWatcher.Value)
            .To((x, v) =>
            {
                x.Log.Debug($"Target value has changed to {v}");
            });
    }
        
    public ReactiveBinding(string targetPropertyPath, IValueProvider sourceValueProvider, IValueWatcher targetWatcher)
    {
        Log = typeof(ReactiveBinding).PrepareLogger().WithSuffix(ToString);
        TargetPropertyPath = targetPropertyPath;
        SourceWatcher = sourceValueProvider.AddTo(Anchors);
        TargetWatcher = targetWatcher.AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
    }
        
    private IFluentLog Log { get; }

    /// <summary>
    /// Gets the path of the target property that this binding is associated with.
    /// </summary>
    public string TargetPropertyPath { get; }

    /// <summary>
    /// Gets the error message, if any, associated with the binding. This property is populated when the binding encounters errors.
    /// </summary>
    public string Error { get; private set; }

    /// <summary>
    /// Gets the source watcher that monitors the source value for changes.
    /// </summary>
    public IValueProvider SourceWatcher { get; }
        
    /// <summary>
    /// Gets the target watcher that reflects changes on the target property.
    /// </summary>
    public IValueWatcher TargetWatcher { get; }
        
    /// <summary>
    /// Gets a value indicating whether the binding is currently active and propagating changes.
    /// </summary>
    public bool IsActive { get; private set; }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("Binding");
        builder.AppendParameter(nameof(IsActive), IsActive ? "active" : "NOT active");
        builder.AppendParameter(nameof(SourceWatcher), SourceWatcher);
        builder.AppendParameter(nameof(TargetWatcher), TargetWatcher);
        builder.AppendParameter(nameof(TargetPropertyPath), TargetPropertyPath);
    }
}
    
/// <summary>
/// Provides a generic implementation of ReactiveBinding, connecting a source and a target with specific property types.
/// This binding enables reactive updates between the specified properties of the source and target objects.
/// </summary>
/// <typeparam name="TSource">The type of the source object.</typeparam>
/// <typeparam name="TTarget">The type of the target object.</typeparam>
/// <typeparam name="TProperty">The type of the property being bound.</typeparam>
internal sealed class ReactiveBinding<TSource, TTarget, TProperty> : ReactiveBinding, IReactiveBinding where TSource : class where TTarget : class
{
    /// <summary>
    /// Initializes a new instance of the ReactiveBinding class using property accessors for the source and target.
    /// </summary>
    /// <param name="targetPropertyAccessor">The lambda expression to access the target property.</param>
    /// <param name="sourceAccessor">The lambda expression to access the source property.</param>
    public ReactiveBinding(Expression<Func<TTarget, TProperty>> targetPropertyAccessor, Expression<Func<TSource, TProperty>> sourceAccessor) 
        : this(targetPropertyAccessor.GetMemberName(), targetPropertyAccessor, sourceAccessor)
    {
            
    }
        
    /// <summary>
    /// Initializes a new instance of the ReactiveBinding class with the specified target property path and property accessors.
    /// </summary>
    /// <param name="targetPropertyPath">The property path of the target.</param>
    /// <param name="targetPropertyAccessor">The lambda expression to access the target property.</param>
    /// <param name="sourceAccessor">The lambda expression to access the source property.</param>
    public ReactiveBinding(string targetPropertyPath, Expression<Func<TTarget, TProperty>> targetPropertyAccessor, Expression<Func<TSource, TProperty>> sourceAccessor) 
        : base(targetPropertyPath, new ExpressionWatcher<TSource, TProperty>(sourceAccessor), new ExpressionWatcher<TTarget, TProperty>(targetPropertyAccessor))
    {
        SourceWatcher = (ExpressionWatcher<TSource, TProperty>)base.SourceWatcher;
        TargetWatcher = (ExpressionWatcher<TTarget, TProperty>)base.TargetWatcher;
    }

    [DoNotNotify]
    public TSource Source
    {
        get => SourceWatcher.Source;
        set => SourceWatcher.Source = value;
    }

    [DoNotNotify]
    public TTarget Target
    {
        get => TargetWatcher.Source;
        set => TargetWatcher.Source = value;
    }

    public new ExpressionWatcher<TSource, TProperty> SourceWatcher { get; }

    public new ExpressionWatcher<TTarget, TProperty> TargetWatcher { get; }

    IValueProvider IReactiveBinding.SourceWatcher => SourceWatcher;

    IValueWatcher IReactiveBinding.TargetWatcher => TargetWatcher;
}