namespace PoeShared.Native;

/// <summary>
/// A matcher that always returns false, effectively rejecting all window handles.
/// </summary>
public sealed class ArrogantWindowTrackerMatcher : IWindowTrackerMatcher
{
    /// <summary>
    /// Determines whether the specified window handle matches.
    /// Always returns false.
    /// </summary>
    /// <param name="windowHandle">The window handle to evaluate.</param>
    /// <returns>False, unconditionally.</returns>
    public bool IsMatch(IWindowHandle windowHandle)
    {
        return false;
    }
}