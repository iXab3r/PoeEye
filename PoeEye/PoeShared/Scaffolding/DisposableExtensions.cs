using System.Reactive;

namespace PoeShared.Scaffolding;

public static class DisposableExtensions
{
    /// <summary>
    /// Converts an IEnumerable of IDisposable into a CompositeDisposable.
    /// </summary>
    /// <param name="disposables">IEnumerable of IDisposable that will be added to the CompositeDisposable.</param>
    /// <returns>A new CompositeDisposable instance containing all disposables from the input collection.</returns>
    public static CompositeDisposable ToCompositeDisposable(this IEnumerable<IDisposable> disposables)
    {
        var result = new CompositeDisposable();
        disposables.ForEach(result.Add);
        return result;
    }
        
    /// <summary>
    /// Assigns an IDisposable instance to a SerialDisposable.
    /// </summary>
    /// <param name="instance">The IDisposable instance to be assigned.</param>
    /// <param name="anchor">The SerialDisposable to which the instance will be assigned.</param>
    /// <typeparam name="T">The type of the object to assign, which must derive from IDisposable.</typeparam>
    /// <returns>The original IDisposable instance after assignment.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the anchor is null.</exception>
    public static T AssignTo<T>(this T instance, SerialDisposable anchor) where T : IDisposable
    {
        Guard.ArgumentNotNull(anchor, nameof(anchor));
        anchor.Disposable = instance;
        return instance;
    }

    /// <summary>
    /// Adds an IDisposable which encapsulates an action to an ICollection of IDisposable.
    /// </summary>
    /// <param name="instance">The ICollection&lt;IDisposable&gt; to which the item will be added.</param>
    /// <param name="action">The action to encapsulate in an IDisposable using Disposable.Create.</param>
    /// <typeparam name="T">The type of the collection, which must be an ICollection&lt;IDisposable&gt;.</typeparam>
    /// <returns>The modified collection with the added IDisposable.</returns>
    public static T Add<T>(this T instance, Action action) where T: ICollection<IDisposable>
    {
        instance.Add(Disposable.Create(action));
        return instance;
    }
    
    /// <summary>
    /// Adds an IDisposable object to an ICollection of IDisposable, and registers a dispose action for it.
    /// </summary>
    /// <param name="instance">The ICollection&lt;IDisposable&gt; to which the item will be added.</param>
    /// <param name="accessor">A function that returns the item to add to the collection.</param>
    /// <typeparam name="T">The type of the collection, which must be an ICollection&lt;IDisposable&gt;.</typeparam>
    /// <typeparam name="TItem">The type of the item to add, which must derive from IDisposable.</typeparam>
    /// <returns>The modified collection with the added item.</returns>
    public static T Add<T, TItem>(this T instance, Func<TItem> accessor) where T: ICollection<IDisposable> where TItem : IDisposable
    {
        Disposable.Create(() =>
        {
            var item = accessor();
            item?.Dispose();
        }).AddTo(instance);
        return instance;
    }
    
    /// <summary>
    /// Adds a set of IDisposable objects to an ICollection of IDisposable, and registers a dispose action for them.
    /// </summary>
    /// <param name="instance">The ICollection&lt;IDisposable&gt; to which to add the items.</param>
    /// <param name="itemsAccessor">A function that returns an IEnumerable of items to add to the collection.</param>
    /// <typeparam name="T">The type of the collection, which must be an ICollection&lt;IDisposable&gt;.</typeparam>
    /// <typeparam name="TItem">The type of the items to add, which must derive from IDisposable.</typeparam>
    /// <returns>The modified collection with the added items.</returns>
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
    
    /// <summary>
    /// Converts an IDisposable object into an IObservable of type Unit.
    /// </summary>
    /// <param name="disposable">The IDisposable object to convert.</param>
    /// <returns>An IObservable of type Unit with the IDisposable encapsulated.</returns>
    public static IObservable<Unit> ToObservable(this IDisposable disposable)
    {
        return Observable.Create<Unit>(_ => disposable);
    }
}