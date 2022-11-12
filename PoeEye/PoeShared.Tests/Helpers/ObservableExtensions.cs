using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace PoeShared.Tests.Helpers;

public static class ObservableExtensions
{
    public static IReadOnlyObservableCollection<T> Listen<T>(this IObservable<T> observable)
    {
        var result = new ReadOnlyObservableCollectionEx<T>();
        observable.Subscribe(result.Add);
        return result;
    }
}