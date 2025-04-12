using System;

namespace PoeShared.Native;

/// <summary>
/// Interface for matching logic used by window trackers.
/// </summary>
public interface IWindowTrackerMatcher
{
    /// <summary>
    /// Determines whether the specified window handle matches the criteria.
    /// </summary>
    /// <param name="window">The window handle to evaluate.</param>
    /// <returns>True if the window matches; otherwise, false.</returns>
    bool IsMatch(IWindowHandle window);
}
