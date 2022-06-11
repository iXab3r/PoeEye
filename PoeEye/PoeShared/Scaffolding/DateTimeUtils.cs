namespace PoeShared.Scaffolding;

public static class DateTimeUtils
{
    public static double ConvertToUnixTimestamp(DateTime date)
    {
        var diff = date - DateTime.UnixEpoch;
        return Math.Floor(diff.TotalSeconds);
    }
    
    public static DateTime ConvertFromUnixTimestamp(double timestamp)
    {
        return DateTime.UnixEpoch.AddSeconds(timestamp);
    }
}