using JetBrains.Annotations;

namespace PoeShared.Scaffolding;
public sealed class RotatingCancellationTokenSource : DisposableReactiveObject
{
    private CancellationTokenSourceState? ctsState;
    private int rentCount;

    public RotatingCancellationTokenSource()
    {
        Disposable.Create(DisposeCurrentCts).AddTo(Anchors);
    }

    public bool IsRented { get; [UsedImplicitly] private set; }

    public IDisposable Rent(out CancellationToken cancellationToken)
    {
        var newState = new CancellationTokenSourceState(new CancellationTokenSource());

        // Only install if currently null
        if (Interlocked.CompareExchange(ref ctsState, newState, null) != null)
        {
            newState.CancellationTokenSource.Dispose();
            newState.Dispose();
            throw new InvalidOperationException("CancellationTokenSource is still in use");
        }

        // Release disposable tied to THIS state, no re-entrancy
        var release = Disposable.Create(() =>
        {
            // Clear only if still ours
            Interlocked.CompareExchange(ref ctsState, null, newState);

            // Single renter semantics: set IsRented/ rentCount to 0
            Interlocked.Exchange(ref rentCount, 0);
            IsRented = false;

            // Dispose CTS explicitly; do NOT call DisposeCurrentCts() here
            newState.CancellationTokenSource.Dispose();
        });
        release.AddTo(newState.Anchors);

        Interlocked.Exchange(ref rentCount, 1);
        IsRented = true;

        cancellationToken = newState.CancellationTokenSource.Token;
        return newState.Anchors;
    }

    public void Cancel()
    {
        var current = Interlocked.Exchange(ref ctsState, null);
        if (current == null) return;

        try
        {
            current.CancellationTokenSource.Cancel();
        }
        finally
        {
            current.CancellationTokenSource.Dispose();
            current.Dispose();
            Interlocked.Exchange(ref rentCount, 0);
            IsRented = false;
        }
    }

    private void DisposeCurrentCts()
    {
        var old = Interlocked.Exchange(ref ctsState, null);
        if (old == null) return;

        // We only dispose CTS here; avoid re-entrancy loops via Anchors
        old.CancellationTokenSource.Dispose();
        old.Dispose();
    }

    private sealed class CancellationTokenSourceState : IDisposable
    {
        public CancellationTokenSourceState(CancellationTokenSource cts)
        {
            Anchors = new CompositeDisposable();
            CancellationTokenSource = cts;
        }

        public CancellationTokenSource CancellationTokenSource { get; }
        public CompositeDisposable Anchors { get; }

        public void Dispose() => Anchors.Dispose();
    }
}
