using System;
using System.Reactive.Disposables;
using System.Threading;

namespace PoeShared.Blazor.Internals;

internal static class ChangeTrackerHolder
{
    private static long instancesCount;
    public static long Instances => instancesCount;
    
    public static IDisposable RecordCreate()
    {
        Interlocked.Increment(ref instancesCount);
        return Disposable.Create(() =>
        {
            Interlocked.Decrement(ref instancesCount);
        });
    }
}