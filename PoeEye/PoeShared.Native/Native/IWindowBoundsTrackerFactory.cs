using System;
using System.Drawing;

namespace PoeShared.Native;

public interface IWindowBoundsTrackerFactory
{
    IObservable<Rectangle?> Track(IWindowHandle windowToTrack);

    IWindowBoundsTracker CreateTracker();
}