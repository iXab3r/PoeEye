using System;
using System.Diagnostics;

namespace PoeShared.Native;

internal sealed class MainWindowTrackerTitleMatcher : IWindowTrackerMatcher
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    private static readonly string ProcessName = CurrentProcess.ProcessName;
    private static readonly int ProcessId = CurrentProcess.Id;
        
    public bool IsMatch(IWindowHandle window)
    {
        if (window.Handle == CurrentProcess.MainWindowHandle)
        {
            return true;
        }

        if (window.ProcessId == ProcessId || window.ParentProcessId == ProcessId)
        {
            return true;
        }

        //"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\111.0.1661.62\msedgewebview2.exe" --embedded-browser-webview=1
        //--webview-exe-name=EyeAuras.exe --webview-exe-version=1.2.4572 --user-data-dir="C:\Users\Xab3r\AppData\Local\EyeAuras.WebView2\EBWebView" --noerrdialogs --embedded-browser-webview-dpi-awareness=2 --disable-features=MojoIpcz --mojo-named-platform-channel-pipe=18316.72520.7367039381620602427
        if (window.CommandLine != null && window.CommandLine.Contains($"--webview-exe-name={CurrentProcess.ProcessName}"))
        {
            return true;
        }

        return false;
    }
}