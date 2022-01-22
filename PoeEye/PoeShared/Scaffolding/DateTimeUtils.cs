using System;

namespace PoeShared.Scaffolding;

public static class DateTimeUtils
{
    private static readonly DateTime Origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    public static double ConvertToUnixTimestamp(DateTime date)
    {
        var diff = date - Origin;
        return Math.Floor(diff.TotalSeconds);
    }
    
    public static DateTime ConvertFromUnixTimestamp(double timestamp)
    {
        return Origin.AddSeconds(timestamp);
    }
}