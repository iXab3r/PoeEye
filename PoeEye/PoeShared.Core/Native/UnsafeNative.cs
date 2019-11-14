using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
                var result = AllowSetForegroundWindow(CurrentProcessId);
                if (!result)
                {
                    var error = Kernel32.GetLastError();
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
    }
}