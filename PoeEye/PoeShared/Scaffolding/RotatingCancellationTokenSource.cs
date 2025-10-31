using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public sealed class RotatingCancellationTokenSource : DisposableReactiveObject
{
    private CancellationTokenSourceState ctsState;
    private long rentCount;

    public RotatingCancellationTokenSource()
    {
        Disposable.Create(() => DisposeCurrentCts()).AddTo(Anchors);
    }
        
    public bool IsRented { get; [UsedImplicitly] private set; }

    public IDisposable Rent(out CancellationToken cancellationToken)
    {
        if (ctsState != null)
        {
            throw new InvalidOperationException("CancellationTokenSource is still in use");
        }

        var current = new CancellationTokenSourceState(new CancellationTokenSource());
        
        var previous = Interlocked.Exchange(ref ctsState, current);
        if (previous != null)
        {
            throw new InvalidOperationException("Another token was in use unexpectedly");
        }
        
        Disposable.Create(() =>
        {
            IsRented = Interlocked.Decrement(ref rentCount) > 0;
            DisposeCurrentCts();
        }).AddTo(current.Anchors);
        IsRented = Interlocked.Increment(ref rentCount) > 0;
        
        cancellationToken = current.CancellationTokenSource.Token;
        return current.Anchors;
    }

    public void Cancel()
    {
        var current = Interlocked.CompareExchange(ref ctsState, null, ctsState);
        if (current == null)
        {
            return;
        }

        current.CancellationTokenSource.Cancel();
        current.Dispose();
    }

    private void DisposeCurrentCts()
    {
        var oldCts = Interlocked.Exchange(ref ctsState, null);
        if (oldCts == null)
        {
            return;
        }

        oldCts.Dispose();
    }

    private sealed class CancellationTokenSourceState : IDisposable
    {
        public CancellationTokenSourceState(CancellationTokenSource cts)
        {
            Anchors = new CompositeDisposable();
            CancellationTokenSource = cts;
            // cts.AddTo(Anchors); //let it be GCed instead
        }

        public CancellationTokenSource CancellationTokenSource { get;  }
        
        public CompositeDisposable Anchors { get; }
        
        public void Dispose()
        {
            Anchors.Dispose();
        }
    }
}