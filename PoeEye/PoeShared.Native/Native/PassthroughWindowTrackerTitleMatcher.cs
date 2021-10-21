using System;

namespace PoeShared.Native
{
    internal sealed class PassthroughWindowTrackerTitleMatcher : IWindowTrackerMatcher
    {
        public bool IsMatch(string title, IntPtr hwnd, int processId)
        {
            return true;
        }
    }
}