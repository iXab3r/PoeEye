using System.Collections.Generic;

namespace PoeShared.WindowSeekers;

/// <summary>
///     Interface for window seekers.
/// </summary>
public interface IWindowSeeker
{
    bool SkipNotVisibleWindows { get; set; }
        
    /// <summary>
    ///     Get the list of matching windows, ordered by priority (optionally).
    /// </summary>
    IReadOnlyCollection<IWindowHandle> Windows { get; }

    /// <summary>
    ///     Refreshes the list of windows.
    /// </summary>
    void Refresh();
}