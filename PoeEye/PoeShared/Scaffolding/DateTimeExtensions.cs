namespace PoeShared.Scaffolding;

public static class DateTimeExtensions
{
    public static bool IsInRange(this DateTimeOffset date, DateTimeOffset start, DateTimeOffset end)
    {
        return date >= start && date <= end;
    }

    public static bool IsInRange(this DateTimeOffset date, DateTimeOffset start, TimeSpan duration)
    {
        return IsInRange(date, start, start + duration);
    }
    
    public static TimeSpan IntersectionDuration(this DateTimeOffset date, TimeSpan duration, DateTimeOffset intervalStart, TimeSpan intervalDuration)
    {
        // Calculate the end times of the intervals
        var end = date + duration;
        var intervalEnd = intervalStart + intervalDuration;

        // Determine the latest start time and earliest end time
        var intersectionStart = date > intervalStart ? date : intervalStart;
        var intersectionEnd = end < intervalEnd ? end : intervalEnd;

        // Calculate the intersection duration
        var intersectionDuration = intersectionEnd - intersectionStart;

        // If the intersection duration is negative, there is no overlap
        if (intersectionDuration < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return intersectionDuration;
    }
}