using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;


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
}