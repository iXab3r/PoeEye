using System;
using System.Collections.Generic;

namespace PoeShared.WindowSeekers;

/// <summary>
///     Base class for window seekers that can populate a list of window handles based on some criteria and with basic
///     filtering.
/// </summary>
public abstract class BaseWindowSeeker : IWindowSeeker
{
    protected BaseWindowSeeker()
    {
        BlacklistedWindows = new HashSet<IntPtr>();
    }

    /// <summary>
    ///     Gets or sets the window handle of the owner.
    /// </summary>
    /// <remarks>
    ///     Windows with this handle will be automatically skipped.
    /// </remarks>
    public ISet<IntPtr> BlacklistedWindows { get; }

    /// <summary>
    ///     Gets or sets whether not visible windows should be skipped.
    /// </summary>
    public bool SkipNotVisibleWindows { get; set; }

    /// <summary>
    ///     Get the matching windows from the last refresh.
    /// </summary>
    public abstract IReadOnlyCollection<IWindowHandle> Windows { get; protected set; }

    public abstract void Refresh();
}