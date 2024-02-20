namespace PoeShared.Scaffolding;

public readonly record struct ValueStopwatch
{
    private static readonly double STimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long startTimestamp;

    private ValueStopwatch(long startTimestamp) => this.startTimestamp = startTimestamp;
    
    public TimeSpan GetElapsedTime() => GetElapsedTime(startTimestamp, GetTimestamp());

    public double ElapsedMilliseconds => GetElapsedTime().TotalMilliseconds;
    
    public double ElapsedTicks => GetTimestamp();
    
    public TimeSpan Elapsed => GetElapsedTime();

    public static ValueStopwatch StartNew() => new(GetTimestamp());

    public static long GetTimestamp() => Stopwatch.GetTimestamp();
    
    public static TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
    {
        var timestampDelta = endTimestamp - startTimestamp;
        var ticks = (long)(STimestampToTicks * timestampDelta);
        return new TimeSpan(ticks);
    }
}