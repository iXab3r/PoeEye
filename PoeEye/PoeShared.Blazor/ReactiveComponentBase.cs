using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Components;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor;

public abstract class ReactiveComponentBase : ComponentBase, IDisposableReactiveObject
{
    private static long GlobalIdx;

    private readonly Lazy<IFluentLog> logSupplier;

    protected ReactiveComponentBase()
    {
        ObjectId = $"Cmp#{Interlocked.Increment(ref GlobalIdx)}";

        logSupplier = new Lazy<IFluentLog>(PrepareLogger);
    }

    protected IFluentLog Log => logSupplier.Value;
    protected string ObjectId { get; }

    protected virtual IFluentLog PrepareLogger()
    {
        return GetType().PrepareLogger().WithSuffix(ObjectId);
    }

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected TRet RaiseAndSetIfChanged<TRet>(ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return newValue;
        }

        return RaiseAndSet(ref backingField, newValue, propertyName);
    }

    protected TRet RaiseAndSet<TRet>(
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string propertyName = null)
    {
        backingField = newValue;
        RaisePropertyChanged(propertyName);
        return newValue;
    }
}