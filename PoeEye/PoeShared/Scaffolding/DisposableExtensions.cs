namespace PoeShared.Scaffolding;

public static class DisposableExtensions
{
    public static CompositeDisposable ToCompositeDisposable(this IEnumerable<IDisposable> disposables)
    {
        var result = new CompositeDisposable();
        disposables.ForEach(result.Add);
        return result;
    }
        
    public static T AssignTo<T>(this T instance, SerialDisposable anchor) where T : IDisposable
    {
        Guard.ArgumentNotNull(anchor, nameof(anchor));
        anchor.Disposable = instance;
        return instance;
    }

    public static T Add<T>(this T instance, Action action) where T: ICollection<IDisposable>
    {
        instance.Add(Disposable.Create(action));
        return instance;
    }
    
    public static T Add<T, TItem>(this T instance, Func<TItem> accessor) where T: ICollection<IDisposable> where TItem : IDisposable
    {
        Disposable.Create(() =>
        {
            var item = accessor();
            item?.Dispose();
        }).AddTo(instance);
        return instance;
    }
    
    public static T Add<T, TItem>(this T instance, Func<IEnumerable<TItem>> itemsAccessor) where T: ICollection<IDisposable> where TItem : IDisposable
    {
        Disposable.Create(() =>
        {
            foreach (var evaluator in itemsAccessor())
            {
                evaluator?.Dispose();
            }
        }).AddTo(instance);
        return instance;
    }
}