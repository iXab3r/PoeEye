namespace PoeShared.Scaffolding;

public static class StopwatchExtensions
{
    public static TimeSpan GetTime(this Stopwatch stopwatch)
    {
        return TimeSpan.FromMilliseconds(Stopwatch.GetTimestamp() / (float) Stopwatch.Frequency);
    }
}