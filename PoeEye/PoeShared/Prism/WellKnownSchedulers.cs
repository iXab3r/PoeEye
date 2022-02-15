namespace PoeShared.Prism;

public static class WellKnownSchedulers
{
    public const string UI = "UiScheduler";
    public const string UIIdle = "UiIdleScheduler";
    public const string Background = "BackgroundScheduler";
    public const string InputHook = "Input";
    public const string SendInput = "SendInput";
    public const string SharedThread = "SharedThread";
    public const string RedirectToUI = "UISchedulerIfNotOnUIThread";
}