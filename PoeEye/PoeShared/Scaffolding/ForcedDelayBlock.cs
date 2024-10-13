namespace PoeShared.Scaffolding;

/// <summary>
/// Enforces a minimum duration for a block of code execution.
/// </summary>
public sealed class ForcedDelayBlock : IDisposable, IAsyncDisposable
{
    private readonly Stopwatch stopwatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForcedDelayBlock"/> class.
    /// </summary>
    /// <param name="delayMs">The minimum time duration the block should take, in milliseconds.</param>
    public ForcedDelayBlock(double delayMs) : this(TimeSpan.FromMilliseconds(delayMs))
    {
    }

    public ForcedDelayBlock(TimeSpan minTime)
    {
        MinTime = minTime;
        stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Gets the minimum execution time for the block.
    /// </summary>
    public TimeSpan MinTime { get; }

    /// <summary>
    /// Gets the elapsed time since the block started execution.
    /// </summary>
    public TimeSpan Elapsed => stopwatch.Elapsed;

    /// <summary>
    /// Ensures the block of code takes at least the specified minimum time to execute. 
    /// If the code finishes earlier, the remaining time is spent sleeping using high-precision synchronous sleep.
    /// </summary>
    public void Dispose()
    {
        var elapsed = stopwatch.Elapsed;
        var timeToSleep = MinTime - elapsed;
        if (timeToSleep > TimeSpan.Zero)
        {
            TaskExtensions.Sleep(timeToSleep);
        }
    }

    /// <summary>
    /// Asynchronously ensures the block of code takes at least the specified minimum time to execute. 
    /// If the code finishes earlier, the remaining time is spent asynchronously using a high-precision sleep method.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var elapsed = stopwatch.Elapsed;
        var timeToSleep = MinTime - elapsed;
        if (timeToSleep > TimeSpan.Zero)
        {
            await Task.Run(() => TaskExtensions.Sleep(timeToSleep));
        }
    }
}
