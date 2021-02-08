using System;

namespace PoeShared
{
    public interface IRandomNumberGenerator
    {
        int Next(int min, int max);

        int Next();

        int Next(int max);
        
        public TimeSpan GenerateDelay(TimeSpan minDelay, TimeSpan maxDelay)
        {
            var minDelayMs = (int)minDelay.TotalMilliseconds;
            var maxDelayMs = (int)maxDelay.TotalMilliseconds;
            var delayMs = minDelayMs != maxDelayMs ? minDelayMs + Next(minDelayMs, maxDelayMs) : minDelayMs;
            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}