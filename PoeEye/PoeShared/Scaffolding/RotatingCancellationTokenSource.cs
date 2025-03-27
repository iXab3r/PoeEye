using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public sealed class RotatingCancellationTokenSource : DisposableReactiveObject
{
    private CancellationTokenSource cts;
    private long rentCount;

    public RotatingCancellationTokenSource()
    {
        Disposable.Create(() => DisposeCurrentCts()).AddTo(Anchors);
    }
        
    public bool IsRented { get; [UsedImplicitly] private set; }

    public IDisposable Rent(out CancellationToken cancellationToken)
    {
        if (cts != null)
        {
            throw new InvalidOperationException("CancellationTokenSource is still in use");
        }
            
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref cts, current);

        if (previous != null)
        {
            throw new InvalidOperationException("Another token was in use unexpectedly");
        }

        IsRented = Interlocked.Increment(ref rentCount) > 0;
        cancellationToken = current.Token;
        return Disposable.Create(() =>
        {
            IsRented = Interlocked.Decrement(ref rentCount) > 0;
            DisposeCurrentCts();
        });
    }

    public void Cancel()
    {
        var current = Interlocked.CompareExchange(ref cts, null, cts);
        if (current == null)
        {
            return;
        }

        current.Cancel();
    }

    private void DisposeCurrentCts()
    {
        var oldCts = Interlocked.Exchange(ref cts, null);
        if (oldCts == null)
        {
            return;
        }

        oldCts.Dispose();
    }
}