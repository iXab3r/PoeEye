namespace PoeShared.Services;

public sealed class SharedResourceLatch : DisposableReactiveObject, ISharedResourceLatch
{
    private static readonly IFluentLog Log = typeof(SharedResourceLatch).PrepareLogger();

    private long counter = 0;

    public bool IsBusy => counter > 0;

    public string Name { get; set; }

    public SharedResourceLatch(string name)
    {
        Name = name;
    }

    public SharedResourceLatch() : this(string.Empty)
    {
    }

    public IDisposable Rent()
    {
        var wasPaused = IsBusy;
        var counterAfterIncrement = Interlocked.Increment(ref counter);
        if (!wasPaused)
        {
            Log.Debug(() => $"[{this}] Marked as busy: {counterAfterIncrement}");
            RaisePropertyChanged(nameof(IsBusy));
        }
        else
        {
            Log.Debug(() => $"[{this}] Already in use: {counterAfterIncrement}");
        }

        return Disposable.Create(() =>
        {
            var counterAfterDecrement = Interlocked.Decrement(ref counter);
            if (!IsBusy)
            {
                Log.Debug(() => $"[{this}] Released: {counterAfterDecrement}");
                RaisePropertyChanged(nameof(IsBusy));
            }
            else
            {
                Log.Debug(() => $"[{this}] Still in use: {counterAfterDecrement}");
            }
        });
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append(nameof(SharedResourceLatch));
        builder.AppendParameter(nameof(Name), Name);
        builder.AppendParameter(nameof(counter), counter);
    }
}