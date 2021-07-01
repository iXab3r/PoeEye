using System;

namespace PoeShared.UI
{
    public static class UiConstants
    {
        public static TimeSpan ArtificialVeryLongDelay = TimeSpan.FromSeconds(30);
        public static TimeSpan ArtificialLongDelay = TimeSpan.FromSeconds(5);
        public static TimeSpan ArtificialShortDelay = TimeSpan.FromSeconds(2);
        public static TimeSpan ArtificialVeryShortDelay = TimeSpan.FromMilliseconds(500);
        public static TimeSpan UiThrottlingDelay = TimeSpan.FromMilliseconds(250);
        public static TimeSpan Day = TimeSpan.FromDays(1);
        public static TimeSpan Week = TimeSpan.FromDays(7);
        public static double WeekInMilliseconds = Week.TotalMilliseconds;
    }
}