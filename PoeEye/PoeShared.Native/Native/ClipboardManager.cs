using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Concurrency;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Annotations;
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
        private static readonly IFluentLog Log = typeof(ClipboardApi).PrepareLogger();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(ClipboardFormat uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(ClipboardFormat uFormat, IntPtr hMem);

        [DllImport("user32.dll")]
        private static extern bool IsClipboardFormatAvailable(ClipboardFormat format);

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        
        [DllImport(nameof(Kernel32), EntryPoint = "RtlCopyMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        // ReSharper disable once InconsistentNaming
        const int GMEM_MOVABLE = 0x0002;

        // ReSharper disable once InconsistentNaming
        const int GMEM_ZEROINIT = 0x0040;

        // ReSharper disable once InconsistentNaming
        const int GHND = GMEM_MOVABLE | GMEM_ZEROINIT;

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
            SetClipboardText(text, ClipboardFormat.CF_UNICODETEXT);
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

        private static void SetClipboardText([NotNull] string message, ClipboardFormat format)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!OpenClipboard(IntPtr.Zero))
            {
                throw new Win32Exception((Win32ErrorCode) Marshal.GetLastWin32Error(), $"Failed to OpenClipboard");
            }

            try
            {
                uint sizeOfChar = format switch
                {
                    ClipboardFormat.CF_TEXT => 1,
                    ClipboardFormat.CF_UNICODETEXT => 2,
                    _ => throw new ArgumentOutOfRangeException(nameof(format), format, $"Unknown clipboard format: {format}")
                };

                var characters = (uint) message.Length;
                var bytes = (characters + 1) * sizeOfChar;


                // IMPORTANT: SetClipboardData requires memory that was acquired with GlobalAlloc using GMEM_MOVABLE.
                var hGlobal = GlobalAlloc(GHND, (UIntPtr) bytes);
                if (hGlobal == IntPtr.Zero)
                {
                    throw new Win32Exception((Win32ErrorCode) Marshal.GetLastWin32Error(), $"Failed to GlobalAlloc data buffer");
                }

                try
                {
                    // IMPORTANT: Marshal.StringToHGlobalUni allocates using LocalAlloc with LMEM_FIXED.
                    //            Note that LMEM_FIXED implies that LocalLock / LocalUnlock is not required.
                    var source = format switch
                    {
                        ClipboardFormat.CF_TEXT => Marshal.StringToHGlobalAnsi(message),
                        ClipboardFormat.CF_UNICODETEXT => Marshal.StringToHGlobalUni(message),
                        _ => throw new ArgumentOutOfRangeException(nameof(format), format, $"Unknown clipboard format: {format}")
                    };

                    try
                    {
                        var target = GlobalLock(hGlobal);
                        if (target == IntPtr.Zero)
                        {
                            throw new Win32Exception((Win32ErrorCode) Marshal.GetLastWin32Error(), $"Failed to GlobalLock data");
                        }

                        try
                        {
                            CopyMemory(target, source, bytes);
                        }
                        finally
                        {
                            var ignore = GlobalUnlock(target);
                        }

                        if (SetClipboardData(format, hGlobal).ToInt64() != 0)
                        {
                            // IMPORTANT: SetClipboardData takes ownership of hGlobal upon success.
                            hGlobal = IntPtr.Zero;
                        }
                        else
                        {
                            throw new Win32Exception((Win32ErrorCode) Marshal.GetLastWin32Error(), $"Failed to SetClipboard data");
                        }
                    }
                    finally
                    {
                        // Marshal.StringToHGlobalUni actually allocates with LocalAlloc, thus we should theorhetically use LocalFree to free the memory...
                        // ... but Marshal.FreeHGlobal actully uses a corresponding version of LocalFree internally, so this works, even though it doesn't
                        //  behave exactly as expected.
                        Marshal.FreeHGlobal(source);
                    }
                }
                finally
                {
                    if (hGlobal != IntPtr.Zero)
                    {
                        var ignore = GlobalFree(hGlobal);
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
    }
}