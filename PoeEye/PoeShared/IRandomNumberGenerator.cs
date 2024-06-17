namespace PoeShared;

public interface IRandomNumberGenerator
{
    int Next(int min, int max);

    int Next();

    int Next(int max);

    double NextDouble();

    public TimeSpan NextTimeSpan(TimeSpan minDelay, TimeSpan maxDelay)
    {
        return GenerateDelay(minDelay, maxDelay);
    }
    
    public TimeSpan GenerateDelay(TimeSpan minDelay, TimeSpan maxDelay)
    {
        var minDelayMs = (int)minDelay.TotalMilliseconds;
        var maxDelayMs = (int)maxDelay.TotalMilliseconds;
        var delayMs = minDelayMs < maxDelayMs ? minDelayMs + Next(maxDelayMs - minDelayMs) : minDelayMs;
        return TimeSpan.FromMilliseconds(delayMs);
    }
}