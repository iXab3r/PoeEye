namespace PoeShared.Launcher;

public enum LauncherMethod
{
    Version,
    /// <summary>
    /// 1) Awaits for target app to exit
    /// 2) Starts executable @ ExecutablePath with arguments
    /// 3) Terminates itself
    /// </summary>
    StartApp,
    /// <summary>
    /// 1) Awaits for target app to exit
    /// 2) Tries to remove old executable @ ExecutablePath
    /// 3) When succeeded, replaced old executable with itself (launcher)
    /// 4) Starts executable @ ExecutablePath with arguments
    /// 5) Terminates itself
    /// </summary>
    SwapApp,
}