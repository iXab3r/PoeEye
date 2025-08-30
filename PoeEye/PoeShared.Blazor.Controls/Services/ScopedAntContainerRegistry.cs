using System;
using System.Threading;

namespace PoeShared.Blazor.Controls.Services;

internal interface IScopedAntContainerRegistry
{
    event Action? Released;
    bool IsHeld { get; }
    bool TryAcquire();
    void Release();
}

internal sealed class ScopedAntContainerRegistry : IScopedAntContainerRegistry
{
    private int held; // 0 = free, 1 = taken
    public event Action? Released;

    public bool IsHeld => Volatile.Read(ref held) == 1;

    public bool TryAcquire()
        => Interlocked.CompareExchange(ref held, 1, 0) == 0;

    public void Release()
    {
        if (Interlocked.Exchange(ref held, 0) == 1)
            Released?.Invoke();
    }
}