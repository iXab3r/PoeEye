﻿using System;

namespace PoeShared.UI;

public static class UiConstants
{
    public static readonly TimeSpan ArtificialVeryLongDelay = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ArtificialLongDelay = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan ArtificialShortDelay = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan ArtificialVeryShortDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan ArtificialDelay = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan UiThrottlingDelay = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan UiThrottlingLongDelay = TimeSpan.FromMilliseconds(1000);
    public static readonly TimeSpan UiThrottlingShortDelay = TimeSpan.FromMilliseconds(0);
    public static readonly TimeSpan UiAnimationDelay = TimeSpan.FromMilliseconds(1000 / 60f);
    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public static readonly TimeSpan Week = TimeSpan.FromDays(7);
    public static readonly int UiThrottlingDelayInMilliseconds = (int)UiThrottlingDelay.TotalMilliseconds;
    public static readonly int UiThrottlingShortDelayInMilliseconds = (int)UiThrottlingShortDelay.TotalMilliseconds;
    public static readonly int UiAnimationDelayInMilliseconds = (int)UiAnimationDelay.TotalMilliseconds;
    public static readonly int UiArtificialDelayInMilliseconds = (int)ArtificialDelay.TotalMilliseconds;
    public static readonly double WeekInMilliseconds = Week.TotalMilliseconds;
    public static readonly double ArtificialVeryLongDelayInMilliseconds = ArtificialVeryLongDelay.TotalMilliseconds;
}