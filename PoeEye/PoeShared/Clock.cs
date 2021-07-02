using System;
using System.Diagnostics;

namespace PoeShared
{
    internal sealed class Clock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
        
        public DateTime Now => DateTime.Now;

        public long Ticks => Stopwatch.GetTimestamp();
        
        public long Frequency => Stopwatch.Frequency;

        public TimeSpan Elapsed => TimeSpan.FromMilliseconds(Ticks / (double) Frequency);
    }
}