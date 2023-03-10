namespace PoeShared.Scaffolding;

public static class StopwatchExtensions
{
    public static TimeSpan GetTime(this Stopwatch stopwatch)
    {
        return TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (float) Stopwatch.Frequency);
    }
}