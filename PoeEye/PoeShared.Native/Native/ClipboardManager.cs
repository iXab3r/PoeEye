using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Native;

internal sealed class ClipboardManager : DisposableReactiveObjectWithLogger, IClipboardManager
{
    private readonly IScheduler uiScheduler;

    public ClipboardManager([Dependency(WellKnownSchedulers.RedirectToUI)] IScheduler uiScheduler)
    {
        this.uiScheduler = uiScheduler;
    }

    public TimeSpan ClipboardRestorationTimeout { get; } = TimeSpan.FromMilliseconds(200);

    public int ClipboardSetRetryCount { get; } = 10;

    public void SetText(string text)
    {
        Guard.ArgumentNotNull(() => text);

        ClipboardApi.SetText(text);
    }

    public bool ContainsText()
    {
        return ClipboardApi.ContainsText();
    }

    public string GetText()
    {
        return ClipboardApi.GetText();
    }

    public IDataObject GetDataObject()
    {
        EnsureUiThread();
        return Clipboard.GetDataObject();
    }

    public void SetDataObject(object dataObject)
    {
        EnsureUiThread();
        Log.Debug(
            $"[PoeChatService] Setting new clipboard object '{dataObject}' (retry: {ClipboardSetRetryCount}, timeout: {ClipboardRestorationTimeout})...");
        Clipboard.SetDataObject(dataObject, true, ClipboardSetRetryCount, (int) ClipboardRestorationTimeout.TotalMilliseconds);
    }

    public bool ContainsFileDropList()
    {
        EnsureUiThread();
        return Clipboard.ContainsFileDropList();
    }

    public IReadOnlyList<string> GetFileDropList()
    {
        EnsureUiThread();
        var files = Clipboard.GetFileDropList();
        var result = new List<string>();
        foreach (var file in files)
        {
            file.AddTo(result);
        }

        return result;
    }

    private static class ClipboardApi
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(ClipboardFormat uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(ClipboardFormat uFormat, IntPtr hMem);
        
        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(ClipboardFormat format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        public enum ClipboardFormat : uint
        {
            /// <summary>
            /// Text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data. Use this format for ANSI text.
            /// </summary>
            CF_TEXT = 1,
            /// <summary>
            /// Unicode text format. Each line ends with a carriage return/linefeed (CR-LF) combination. A null character signals the end of the data.
            /// </summary>
            CF_UNICODETEXT = 13
        }

        public static bool ContainsText() => ContainsFormat(ClipboardFormat.CF_UNICODETEXT);

        public static bool ContainsFormat(ClipboardFormat format)
        {
            return IsClipboardFormatAvailable(format);
        }

        public static void SetText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return;
            }

            try
            {
                var lpwcstr = Marshal.StringToHGlobalUni(text);
                try
                {
                    SetClipboardData(ClipboardFormat.CF_UNICODETEXT, lpwcstr);
                }
                finally
                {
                    Marshal.FreeHGlobal(lpwcstr);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static T GetData<T>(ClipboardFormat format, Func<IntPtr, T> converter)
        {
            if (!ContainsFormat(format))
            {
                return default;
            }

            if (!OpenClipboard(IntPtr.Zero))
            {
                return default;
            }

            try
            {
                var hGlobal = GetClipboardData(format);
                return hGlobal == IntPtr.Zero ? default : converter(hGlobal);
            }
            finally
            {
                CloseClipboard();
            }
        }
        
        public static string GetText()
        {
            return GetData(ClipboardFormat.CF_UNICODETEXT, ptr =>
            {
                var lpwcstr = GlobalLock(ptr);
                try
                {
                    if (lpwcstr != IntPtr.Zero)
                    {
                        return Marshal.PtrToStringUni(lpwcstr);
                    }

                    return default;
                }
                finally
                {
                    GlobalUnlock(lpwcstr);
                }
            });
        }
    }
}