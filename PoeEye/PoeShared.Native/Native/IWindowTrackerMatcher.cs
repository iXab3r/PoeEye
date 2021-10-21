using System;

namespace PoeShared.Native
{
    public interface IWindowTrackerMatcher
    {
        bool IsMatch(string title, IntPtr hwnd, int processId);
    }
}