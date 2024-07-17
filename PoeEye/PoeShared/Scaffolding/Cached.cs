namespace PoeShared.Scaffolding;

public sealed record Cached<T>
{
    private readonly TimeSpan ttl;
    private readonly Stopwatch stopwatch;
    private double expiryMs;
    private int initialized;

    public Cached(TimeSpan ttl)
    {
        this.ttl = ttl;
        this.stopwatch = Stopwatch.StartNew();
        this.expiryMs = 0;
        this.initialized = 0;
    }

    public bool IsExpired => CalculateIsExpired(stopwatch.ElapsedMilliseconds);

    public T Value { get; private set; }

    public T Refresh(T value)
    {
        return Refresh(() => value);
    }
    
    public T Refresh(Func<T> valueFactory)
    {
        var currentMilliseconds = stopwatch.ElapsedMilliseconds;
        return RefreshValue(currentMilliseconds, valueFactory);
    }

    public T GetOrRefresh(Func<T> valueFactory)
    {
        var currentMilliseconds = stopwatch.ElapsedMilliseconds;
        if (CalculateIsExpired(currentMilliseconds))
        {
            return RefreshValue(currentMilliseconds, valueFactory);
        }
        else
        {
            return Value;
        }
    }

    private bool CalculateIsExpired(long currentMilliseconds)
    {
        return Volatile.Read(ref expiryMs) <= currentMilliseconds;
    }

    private T RefreshValue(long currentMs, Func<T> valueFactory)
    {
        if (Interlocked.CompareExchange(ref initialized, 1, 0) == 0)
        {
            try
            {
                var newValue = valueFactory();
                var newExpiryMilliseconds = currentMs + ttl.TotalMilliseconds;

                Value = newValue;
                Volatile.Write(ref expiryMs, newExpiryMilliseconds);
                return newValue;
            }
            finally
            {
                Interlocked.Exchange(ref initialized, 0);
            }
        }
        else
        {
            var spinWait = new SpinWait();
            while (Volatile.Read(ref initialized) != 0)
            {
                spinWait.SpinOnce();
                if (spinWait.NextSpinWillYield)
                {
                    Thread.Yield();
                }
            }

            return Value;
        }
    }
}