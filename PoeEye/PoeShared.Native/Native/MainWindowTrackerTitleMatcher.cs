using System;
using System.Diagnostics;

namespace PoeShared.Native
{
    internal sealed class MainWindowTrackerTitleMatcher : IWindowTrackerMatcher
    {
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();
        
        public bool IsMatch(string title, IntPtr hwnd, int processId)
        {
            return hwnd == CurrentProcess.MainWindowHandle || processId == CurrentProcess.Id;
        }
    }
}