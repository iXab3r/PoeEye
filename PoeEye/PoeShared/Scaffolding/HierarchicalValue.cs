using JetBrains.Annotations;
using LinqKit;
using PropertyBinder;

namespace PoeShared.Scaffolding;

public sealed class HierarchicalValue<TContainer, T> : DisposableReactiveObject where TContainer : class
{
    public HierarchicalValue(
        TContainer owner,
        Expression<Func<TContainer, T>> valueExtractor, 
        Expression<Func<TContainer, TContainer>> parentExtractor,
        Expression<Func<TContainer, T, T>> valueCalculator)
    {
        Owner = owner;
        var binder = new Binder<HierarchicalValue<TContainer, T>>();

        Expression<Func<HierarchicalValue<TContainer, T>, T>> ownerValueExpression = x => valueExtractor.Invoke(x.Owner);
        binder
            .BindIf(x => x.Owner != null, ownerValueExpression.Expand())
            .Else(x => default)
            .To(x => x.OwnerValue);
        
        Expression<Func<HierarchicalValue<TContainer, T>, TContainer>> parentExpression = x => parentExtractor.Invoke(x.Owner);
        binder
            .BindIf(x => x.Owner != null, parentExpression.Expand())
            .Else(x => default)
            .To(x => x.Parent);
        
        Expression<Func<HierarchicalValue<TContainer, T>, T>> parentValueExpression = x => valueExtractor.Invoke(x.Parent);
        binder
            .BindIf(x => x.Parent != null, parentValueExpression.Expand())
            .Else(x => default)
            .To(x => x.ParentValue);

        Expression<Func<HierarchicalValue<TContainer, T>, T>> calculatorExpression = x => valueCalculator.Invoke(x.Parent, x.OwnerValue); 
        
        binder
            .BindIf(x => x.Parent != null, calculatorExpression.Expand())
            .Else(x => x.OwnerValue)
            .To(x => x.CalculatedValue);
        
        binder.Attach(this).AddTo(Anchors);
    }

    public TContainer Owner { get; }
    
    public TContainer Parent { get; [UsedImplicitly] private set; }
    
    public T CalculatedValue { get; [UsedImplicitly] private set; }
    
    public T OwnerValue { get; [UsedImplicitly] private set; }
    
    public T ParentValue { get; [UsedImplicitly] private set; }
}