using System.Linq.Expressions;
using PropertyBinder;
using PropertyChanged;

namespace PoeShared.Bindings;

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
                x.Log.Debug(() => $"Propagating value {v ?? "NULL"}");
                x.TargetWatcher.SetCurrentValue(v);
            });
            
            
        Binder.BindIf(x => x.TargetWatcher.HasValue, x => x.TargetWatcher.Value)
            .To((x, v) =>
            {
                x.Log.Debug(() => $"Target value has changed to {v}");
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

    public string TargetPropertyPath { get; }
        
    public string Error { get; private set; }

    public IValueProvider SourceWatcher { get; }
        
    public IValueWatcher TargetWatcher { get; }
        
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
    
internal sealed class ReactiveBinding<TSource, TTarget, TProperty> : ReactiveBinding, IReactiveBinding where TSource : class where TTarget : class
{
    public ReactiveBinding(Expression<Func<TTarget, TProperty>> targetPropertyAccessor, Expression<Func<TSource, TProperty>> sourceAccessor) 
        : this(targetPropertyAccessor.GetMemberName(), targetPropertyAccessor, sourceAccessor)
    {
            
    }
        
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