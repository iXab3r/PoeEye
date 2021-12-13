using System;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Interop;
using log4net;
using PInvoke;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF
{
    public static class WindowExtensions {
        private static readonly IFluentLog Log = typeof(WindowExtensions).PrepareLogger();

        public static IDisposable RegisterWndProc(this Window instance, HwndSourceHook hook)
        {
            var hwnd = new WindowInteropHelper(instance).EnsureHandle();
            var hwndSource = HwndSource.FromHwnd(hwnd) ?? throw new ApplicationException($"Something went wrong - failed to create {nameof(HwndSource)} for handle {hwnd.ToHexadecimal()}");
            Log.Info($"Adding hook to {instance}");
            hwndSource.AddHook(hook);
            return Disposable.Create(() =>
            {
                Log.Info($"Removing hook from {instance}");
                hwndSource.RemoveHook(hook);
            });
        }

        public static IDisposable LogWndProc(this Window instance, string prefix)
        {
            Log.Info($"[{prefix}] Registering log hook to {instance}");
            return RegisterWndProc(instance,
                (IntPtr hwnd, int msgRaw, IntPtr param, IntPtr lParam, ref bool handled) =>
                {
                    var msg = (User32.WindowMessage) msgRaw;
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(() => $"[{prefix}] Message: {msg} ({msgRaw.ToHexadecimal()} = {msgRaw})");
                    }
                    return IntPtr.Zero;
                });
        }
    }
}