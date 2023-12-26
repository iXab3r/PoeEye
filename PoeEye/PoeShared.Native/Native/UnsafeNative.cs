using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Windows.Input;
using PInvoke;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Native;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Trying to comply to WinAPI naming")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Trying to comply to WinAPI naming")]
public partial class UnsafeNative
{
    private static readonly IFluentLog Log = typeof(UnsafeNative).PrepareLogger();

    private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
    private static readonly Version OSVersion = Environment.OSVersion.Version;

    [SupportedOSPlatform("windows")]
    public static bool IsElevated()
    {
        using (var identity = WindowsIdentity.GetCurrent())
        {
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static void FreeConsole()
    {
        if (!Kernel32.FreeConsole())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    
    public static void AttachConsole()
    {
        const int ATTACH_PARENT_PROCESS = -1;
        if (!Kernel32.AttachConsole(ATTACH_PARENT_PROCESS))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public static void AllocConsole()
    {
        if (!Kernel32.AllocConsole())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        //https://developercommunity.visualstudio.com/content/problem/12166/console-output-is-gone-in-vs2017-works-fine-when-d.html
        // Console.OpenStandardOutput eventually calls into GetStdHandle. As per MSDN documentation of GetStdHandle: http://msdn.microsoft.com/en-us/library/windows/desktop/ms683231(v=vs.85).aspx will return the redirected handle and not the allocated console:
        // "The standard handles of a process may be redirected by a call to  SetStdHandle, in which case  GetStdHandle returns the redirected handle. If the standard handles have been redirected, you can specify the CONIN$ value in a call to the CreateFile function to get a handle to a console's input buffer. Similarly, you can specify the CONOUT$ value to get a handle to a console's active screen buffer."
        // Get the handle to CONOUT$.    
        var stdOutHandle = Kernel32.CreateFile(
            "CONOUT$",
            Kernel32.ACCESS_MASK.GenericRight.GENERIC_READ | Kernel32.ACCESS_MASK.GenericRight.GENERIC_WRITE,
            Kernel32.FileShare.None,
            0,
            Kernel32.CreationDisposition.OPEN_EXISTING,
            default,
            Kernel32.SafeObjectHandle.Null);

        if (stdOutHandle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!Kernel32.SetStdHandle(Kernel32.StdHandle.STD_OUTPUT_HANDLE, stdOutHandle.DangerousGetHandle()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var standardOutput = new StreamWriter(Console.OpenStandardOutput());
        standardOutput.AutoFlush = true;
        Console.SetOut(standardOutput);
    }

    public static void AllowSetForegroundWindow()
    {
        try
        {
            Log.Debug($"Calling AllowSetForegroundWindow(pid: {CurrentProcessId})");
            Win32ErrorCode error;
            if (!AllowSetForegroundWindow(CurrentProcessId) && (error = Kernel32.GetLastError()) != Win32ErrorCode.NERR_Success)
            {
                Log.Error($"AllowSetForegroundWindow has failed for process {CurrentProcessId}, error - {error}!");
                throw new Win32Exception(error, $"Failed to {nameof(AllowSetForegroundWindow)}");
            }

            Log.Debug($"Successfully executed AllowSetForegroundWindow(pid: {CurrentProcessId})");
        }
        catch (Exception e)
        {
            Log.HandleException(e);
        }
    }

    public static string QueryFullProcessImageName(int processId)
    {
        using (var openProcessHandle = OpenProcess(processId))
        {
            var result = new StringBuilder(1024);
            var processPathLength = result.Capacity;

            if (Kernel32.QueryFullProcessImageName(openProcessHandle, Kernel32.QueryFullProcessImageNameFlags.None, result, ref processPathLength) || processPathLength == 0)
            {
                return result.ToString(0, processPathLength);
            }

            result.Clear();
            if (!Kernel32.QueryFullProcessImageName(openProcessHandle, Kernel32.QueryFullProcessImageNameFlags.PROCESS_NAME_NATIVE, result, ref processPathLength) || processPathLength == 0)
            {
                var lastError = Kernel32.GetLastError();
                throw new Win32Exception(lastError, $"Failed to {nameof(QueryFullProcessImageName)} for processId: {processId}");
            }

            return result.ToString(0, processPathLength);
        }
    }

    public static void GetProcessTimes(int processId, out DateTime creationTime, out DateTime exitTime, out DateTime kernelTime, out DateTime userTime)
    {
        using (var openProcessHandle = OpenProcess(processId))
        {
            if (!Kernel32.GetProcessTimes(openProcessHandle, out var creation, out var exit, out var kernel, out var user))
            {
                var lastError = Kernel32.GetLastError();
                throw new Win32Exception(lastError, $"Failed to retrieve process times for processId: {processId}, error code: {lastError}");
            }

            creationTime = DateTime.FromFileTime(creation);
            exitTime = DateTime.FromFileTime(exit);
            kernelTime = DateTime.FromFileTime(kernel);
            userTime = DateTime.FromFileTime(user);
        }
    }

    private static Kernel32.SafeObjectHandle OpenProcess(int processId, uint access = Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION)
    {
        var openProcessHandle = Kernel32.OpenProcess(access, false, processId);
        if (openProcessHandle == null || openProcessHandle.IsInvalid || openProcessHandle.IsClosed)
        {
            var lastError = Kernel32.GetLastError();
            throw new Win32Exception(lastError, $"Failed to open process: {processId}, error code: {lastError}");
        }

        return openProcessHandle;
    }

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    [DllImport("shell32.dll", EntryPoint = "CommandLineToArgvW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr _CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);

    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    public static string[] CommandLineToArgvW(string cmdLine)
    {
        if (string.IsNullOrWhiteSpace(cmdLine))
        {
            return Array.Empty<string>();
        }

        var argv = IntPtr.Zero;
        try
        {
            argv = _CommandLineToArgvW(cmdLine, out var numArgs);
            if (argv == IntPtr.Zero)
            {
                var lastError = Kernel32.GetLastError();
                throw new Win32Exception(lastError, $"Failed parse command line {cmdLine}, error code: {lastError}");
            }

            var result = new string[numArgs];

            for (var i = 0; i < numArgs; i++)
            {
                var currArg = Marshal.ReadIntPtr(argv, i * Marshal.SizeOf(typeof(IntPtr)));
                result[i] = Marshal.PtrToStringUni(currArg);
            }

            return result;
        }
        finally
        {
            if (argv != IntPtr.Zero)
            {
                Kernel32.LocalFree(argv);
            }
        }
    }

    public static ModifierKeys GetCurrentModifierKeys()
    {
        var modifier = ModifierKeys.None;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            modifier |= ModifierKeys.Control;
        }

        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        {
            modifier |= ModifierKeys.Alt;
        }

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            modifier |= ModifierKeys.Shift;
        }

        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        {
            modifier |= ModifierKeys.Windows;
        }

        return modifier;
    }
}