using System;

namespace PoeShared.Native
{
    internal sealed class WindowTrackerTitleMatcher : IWindowTrackerMatcher
    {
        private readonly IStringMatcher titleMatcher;

        public WindowTrackerTitleMatcher(IStringMatcher titleMatcher)
        {
            this.titleMatcher = titleMatcher;
        }

        public bool IsMatch(string title, IntPtr hwnd, int processId)
        {
            return titleMatcher.IsMatch(title);
        }
    }
}