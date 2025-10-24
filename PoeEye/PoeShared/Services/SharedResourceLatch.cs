#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoeShared.Services;

public sealed class SharedResourceLatch : ISharedResourceLatch
{
    private static readonly IFluentLog Log = typeof(SharedResourceLatch).PrepareLogger();
    private static readonly PropertyChangedEventArgs IsBusyEventArgs = new(nameof(IsBusy));

    private long counter;

    /// <summary>
    /// Need to cache it to avoid allocation
    /// </summary>
    private readonly Action releaseAction;
    
    public SharedResourceLatch(string name)
    {
        Name = name;
        releaseAction = Release;
    }

    public SharedResourceLatch() : this(string.Empty)
    {
    }

    public CompositeDisposable Anchors { get; } = new();

    public bool IsBusy => Volatile.Read(ref counter) > 0;

    public string? Name { get; }
    
    public IDisposable Rent()
    {
        var counterAfterIncrement = Interlocked.Increment(ref counter);
        if (counterAfterIncrement == 1) //notify only if we switched from 0 to 1, everyone else won't be notified
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug($"[{this}] Marked as busy: {counterAfterIncrement}");
            }

            RaisePropertyChanged(IsBusyEventArgs);
        }
        else
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug($"[{this}] Already in use: {counterAfterIncrement}");
            }
        }

        return DisposableAction.Create(releaseAction);
    }

    public void Dispose()
    {
        if (Anchors.IsDisposed)
        {
            return;
        }

        Anchors.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Release()
    {
        var after = Interlocked.Decrement(ref counter);
        if (after == 0)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug($"[{this}] Released: {after}");
            }

            RaisePropertyChanged(IsBusyEventArgs);
        }
        else
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug($"[{this}] Still in use: {after}");
            }
        }
    }

    public override string ToString()
    {
        var builder = new ToStringBuilder(this);
        builder.Append(nameof(SharedResourceLatch));
        builder.AppendParameter(nameof(Name), Name);
        builder.AppendParameter(nameof(counter), Volatile.Read(ref counter));
        return builder.ToString();
    }

    private void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }
}