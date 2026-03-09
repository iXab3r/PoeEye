using System.Runtime.InteropServices;

namespace PoeShared.WinCaptureAudio.Extensions;

internal static class Kernel32Api
{
    [DllImport("kernel32.dll")]
    public static extern nint CreateEvent(nint lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

    [DllImport("kernel32.dll")]
    public static extern int WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    public static extern int WaitForMultipleObjects(uint nCount, nint[] lpHandles, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetEvent(nint hEvent);
}