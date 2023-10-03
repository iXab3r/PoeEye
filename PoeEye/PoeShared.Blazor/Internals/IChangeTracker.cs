using System;

namespace PoeShared.Blazor.Internals;

internal interface IChangeTracker : IDisposable
{
    long Revision { get; }

    string StampExpression { get; }

    IObservable<object> WhenChanged { get; }
}