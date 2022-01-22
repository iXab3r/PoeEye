using System;
using System.Diagnostics;

namespace PoeShared;

internal sealed class Clock : IClock
{
    private readonly Stopwatch sw = Stopwatch.StartNew();
        
    public DateTime UtcNow => DateTime.UtcNow;
        
    public DateTime Now => DateTime.Now;

    public long Ticks => sw.ElapsedTicks;
        
    public long Frequency => Stopwatch.Frequency;

    public TimeSpan Elapsed => sw.Elapsed;
}