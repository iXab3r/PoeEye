using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PoeShared.Squirrel.Native
{
    internal static class NativeMethods
    {
        [DllImport("version.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileVersionInfo(
            string lpszFileName,
            int dwHandleIgnored,
            int dwLen,
            [MarshalAs(UnmanagedType.LPArray)] byte[] lpData);

        [DllImport("version.dll", SetLastError = true)]
        internal static extern int GetFileVersionInfoSize(
            string lpszFileName,
            IntPtr dwHandleIgnored);

        [DllImport("version.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VerQueryValue(
            byte[] pBlock,
            string pSubBlock,
            out IntPtr pValue,
            out int len);

        [DllImport("psapi.dll", SetLastError = true)]
        internal static extern bool EnumProcesses(
            IntPtr pProcessIds, // pointer to allocated DWORD array
            int cb,
            out int pBytesReturned);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            [In] int justPassZeroHere,
            [Out] StringBuilder lpImageFileName,
            [In] [MarshalAs(UnmanagedType.U4)] ref int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(
            ProcessAccess processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hHandle);
    }
}