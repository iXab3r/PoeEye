using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using log4net;
using PInvoke;

namespace PoeShared.Native
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Trying to comply to WinAPI naming")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Trying to comply to WinAPI naming")]
    public partial class UnsafeNative
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UnsafeNative));

        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly Version OSVersion = Environment.OSVersion.Version;

        public static bool IsElevated()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
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
                    throw new ApplicationException($"AllowSetForegroundWindow has failed - {error} !");
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
            using (var openProcessHandle = Kernel32.OpenProcess(Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION, false, processId))
            {
                if (openProcessHandle == null || openProcessHandle.IsInvalid || openProcessHandle.IsClosed)
                {
                    var lastError = Kernel32.GetLastError();
                    throw new ApplicationException($"Failed to OpenProcess for processId: {processId}, error code: {lastError}", new Win32Exception((int)lastError));
                }
                var result = new StringBuilder(1024);
                var processPathLength = result.Capacity;
                if (!Kernel32.QueryFullProcessImageName(openProcessHandle, Kernel32.QueryFullProcessImageNameFlags.PROCESS_NAME_NATIVE, result, ref processPathLength) || processPathLength == 0)
                {
                    var lastError = Kernel32.GetLastError();
                    throw new ApplicationException($"Failed to retrieve process name for processId: {processId}, error code: {lastError}", new Win32Exception((int)lastError));
                }

                return result.ToString(0, processPathLength);
            }
        }
    }
}