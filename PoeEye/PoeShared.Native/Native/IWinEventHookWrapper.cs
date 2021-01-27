using System;
using JetBrains.Annotations;
using PInvoke;

namespace PoeShared.Native
{
    public interface IWinEventHookWrapper
    {
        [NotNull]
        IObservable<(IntPtr hWinEventHook,
            User32.WindowsEventHookType @event,
            IntPtr hwnd,
            int idObject,
            int idChild,
            int dwEventThread,
            uint dwmsEventTime)> WhenWindowEventTriggered { get; }
    }
}