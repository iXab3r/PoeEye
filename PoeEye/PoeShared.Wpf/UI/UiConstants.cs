using System;

namespace PoeShared.UI
{
    public static class UiConstants
    {
        public static readonly TimeSpan ArtificialVeryLongDelay = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ArtificialLongDelay = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan ArtificialShortDelay = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan ArtificialVeryShortDelay = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan UiThrottlingDelay = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan UiAnimationDelay = TimeSpan.FromMilliseconds(1000 / 60f);
        public static readonly TimeSpan Day = TimeSpan.FromDays(1);
        public static readonly TimeSpan Week = TimeSpan.FromDays(7);
        public static readonly double WeekInMilliseconds = Week.TotalMilliseconds;
        public static readonly double ArtificialVeryLongDelayInMilliseconds = ArtificialVeryLongDelay.TotalMilliseconds;
    }
}