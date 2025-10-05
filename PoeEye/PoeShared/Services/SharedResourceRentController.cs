using System.Reactive.Subjects;

namespace PoeShared.Services;

public sealed class SharedResourceRentController : DisposableReactiveObject, ISharedResourceRentController
{
    private readonly ConcurrentDictionary<long, string> rentals = new();
    private readonly BehaviorSubject<AnnotatedBoolean> isRentedSubject;
    
    private long keyCounter = 0;
    private long usageCounter = 0;

    public SharedResourceRentController() : this(name: string.Empty)
    {
    }

    public SharedResourceRentController(string name)
    {
        isRentedSubject = new BehaviorSubject<AnnotatedBoolean>(default);
        Name = name;
        WhenRented = isRentedSubject.DistinctUntilChanged().AsObservable();
        WhenRented.Subscribe(x =>
        {
            RaisePropertyChanged(nameof(IsRentedState));
            RaisePropertyChanged(nameof(IsRented));
        }).AddTo(Anchors);
    }
    
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public AnnotatedBoolean IsRentedState => isRentedSubject.Value;
    
    /// <inheritdoc />
    public bool IsRented => IsRentedState.Value;
    
    /// <inheritdoc />
    public IObservable<AnnotatedBoolean> WhenRented { get; }
    
    /// <inheritdoc />
    public IDisposable Rent(string reason)
    {
        var key = Interlocked.Increment(ref keyCounter);
        var usagesBeforeRent = Interlocked.Increment(ref usageCounter);
        if (!rentals.TryAdd(key, reason))
        {
            throw new InvalidStateException($"Failed to add item with key {key} to rentals map");
        }

        NotifyIsRentedStatus(usagesBeforeRent);

        return Disposable.Create(() =>
        {
            if (!rentals.TryRemove(key, out var _))
            {
                throw new InvalidStateException($"Failed to remove item with key {key} from rentals map");
            }
            
            var usagesAfterRent = Interlocked.Decrement(ref usageCounter);
            NotifyIsRentedStatus(usagesAfterRent);
        });
    }

    private void NotifyIsRentedStatus(long usages)
    {
        var isRented = usages > 0;
        //there is a race condition - reasons list could be in de-sync, but the most important part is rental status, not reasons
        var reasons = isRented ? string.Join(Environment.NewLine, rentals.Values) : null;
        var rentalStatus = new AnnotatedBoolean(isRented, reasons);
        isRentedSubject.OnNext(rentalStatus);
    }
}