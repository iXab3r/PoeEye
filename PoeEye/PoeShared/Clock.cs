namespace PoeShared;

internal sealed class Clock : IClock
{
    public static IClock Instance { get; } = new Clock();

    private readonly Stopwatch sw = Stopwatch.StartNew();
        
    public DateTime UtcNow => DateTime.UtcNow;
        
    public DateTime Now => DateTime.Now;

    public long Ticks => sw.ElapsedTicks;
        
    public long Frequency => Stopwatch.Frequency;

    public TimeSpan Elapsed => sw.Elapsed;
}