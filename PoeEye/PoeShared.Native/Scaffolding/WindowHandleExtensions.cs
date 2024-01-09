using System;
using System.Diagnostics;
using System.Drawing;
using PInvoke;

namespace PoeShared.Scaffolding;

public static class WindowHandleExtensions
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    private static readonly string ProcessName = CurrentProcess.ProcessName;
    private static readonly int ProcessId = CurrentProcess.Id;

    /// <summary>
    ///   Checks Visibility, WS_CHILD, WS_EX_TOOLWINDOW and other properties to make sure that this window could be interacted with
    /// </summary>
    /// <returns></returns>
    public static bool IsVisibleAndValid(this IWindowHandle windowHandle, bool excludeMinimized = false)
    {
        if (windowHandle.Handle == IntPtr.Zero)
        {
            return false;
        }

        if (windowHandle.Handle == User32.GetShellWindow())
        {
            return false;
        }

        if (excludeMinimized)
        {
            if (!windowHandle.IsVisible)
            {
                return false;
            }
            
            if (windowHandle.IsIconic)
            {
                return false;
            }
            
            if (windowHandle.ClientRect.Width <= 0 || windowHandle.ClientRect.Height <= 0)
            {
                return false;
            }
        }
            
        if (User32.GetAncestor(windowHandle.Handle, User32.GetAncestorFlags.GA_ROOT) != windowHandle.Handle)
        {
            return false;
        }
            
        if (windowHandle.WindowStylesEx.HasFlag(User32.WindowStylesEx.WS_EX_TOOLWINDOW))
        {
            return false;
        }

        if (windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_CHILD) || windowHandle.WindowStyle.HasFlag(User32.WindowStyles.WS_DISABLED))
        {
            return false;
        }
            
        if (UnsafeNative.DwmGetWindowAttribute(windowHandle.Handle, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_CLOAKED))
        {
            return false;
        }

        return true;
    }
    
    public static bool IsOwnWindow(this IWindowHandle window)
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