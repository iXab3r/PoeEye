using System;

namespace PoeShared.Native;

/// <summary>
/// A matcher that always returns true, effectively allowing all window handles.
/// </summary>
public sealed class PassthroughWindowTrackerMatcher : IWindowTrackerMatcher
{
    /// <summary>
    /// Determines whether the specified window handle matches.
    /// Always returns true.
    /// </summary>
    /// <param name="windowHandle">The window handle to evaluate.</param>
    /// <returns>True, unconditionally.</returns>
    public bool IsMatch(IWindowHandle windowHandle)
    {
        return true;
    }
}