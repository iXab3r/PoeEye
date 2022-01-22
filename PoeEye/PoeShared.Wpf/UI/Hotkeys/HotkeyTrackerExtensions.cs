namespace PoeShared.UI;

public static class HotkeyTrackerExtensions
{
    public static IHotkeyListener Listen(this IHotkeyTracker hotkeyTracker)
    {
        return new HotkeyListener(hotkeyTracker);
    }
}