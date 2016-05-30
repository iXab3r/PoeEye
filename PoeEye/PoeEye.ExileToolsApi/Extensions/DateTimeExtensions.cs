using System;

namespace PoeEye.ExileToolsApi.Extensions
{
    internal static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        public static DateTime ToUnixTimeStamp(this long unixTimeStampInMilliseconds)
        {
            return UnixEpochStart.AddMilliseconds(unixTimeStampInMilliseconds).ToLocalTime(); ;
        }

        public static long ToUnixTimeStampInMilliseconds(this DateTime dateTime)
        {
            return (long)(TimeZoneInfo.ConvertTimeToUtc(dateTime) - UnixEpochStart).TotalMilliseconds;
        }
    }
}