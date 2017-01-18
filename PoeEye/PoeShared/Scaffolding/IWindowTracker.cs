using System;

namespace PoeShared.Scaffolding
{
    public interface IWindowTracker
    {
        bool IsActive { get; }

        IntPtr WindowHandle { get; }

        string ActiveWindowTitle { get; }
    }
}