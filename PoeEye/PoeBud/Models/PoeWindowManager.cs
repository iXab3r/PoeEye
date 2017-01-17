using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeBud.Models
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using ReactiveUI;

    internal sealed class PoeWindowManager : DisposableReactiveObject, IPoeWindowManager
    {
        private readonly IFactory<IPoeWindow, IntPtr> windowsFactory;

        public PoeWindowManager(
            [NotNull] IFactory<IPoeWindow, IntPtr> windowsFactory,
            [NotNull] IPoeBudConfigProvider<IPoeBudConfig> configProvider)
        {
            Guard.ArgumentNotNull(() => windowsFactory);
            Guard.ArgumentNotNull(() => configProvider);
            
            this.windowsFactory = windowsFactory;
            
            NativeMethods.WindowSizeOrLocationChanged += WindowSizeOrLocationChanged;

            var recheckPeriod = configProvider.Load().ForegroundWindowRecheckPeriod;
            Log.Instance.Debug($"[PoeWindowManager] RecheckPeriod: {recheckPeriod}");

            Observable
                .Timer(DateTimeOffset.Now, recheckPeriod)
                .Select(_ => NativeMethods.GetForegroundWindow())
                .DistinctUntilChanged()
                .Subscribe(WindowActivated)
                .AddTo(Anchors);
        }

        private void WindowSizeOrLocationChanged(object sender, NativeMethods.NativeEventArgs nativeEventArgs)
        {
            if (activeWindow == null || activeWindow.NativeWindowHandle != nativeEventArgs.Handle)
            {
                return;
            }
            this.RaisePropertyChanged(nameof(ActiveWindow));
        }

        private void WindowActivated(IntPtr activeWindowHandle)
        {
            var activeWindowTitle = NativeMethods.GetWindowTitle(activeWindowHandle);
            Log.Instance.Debug($"[PoeWindowManager] Active window: '{activeWindowTitle}'");
            if (string.IsNullOrWhiteSpace(activeWindowTitle) || !PoeWindowMatcher.IsMatch(activeWindowTitle))
            {
                ActiveWindow = null;
                return;
            }

            ActiveWindow = windowsFactory.Create(activeWindowHandle);
        }

        private IPoeWindow activeWindow;

        public IPoeWindow ActiveWindow
        {
            get { return activeWindow; }
            set { this.RaiseAndSetIfChanged(ref activeWindow, value); }
        }

        public Regex PoeWindowMatcher { get; } = new Regex("^Path of Exile$", RegexOptions.Compiled);

        private static class NativeMethods
        {
            private const uint WINEVENT_OUTOFCONTEXT = 0;
            private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

            private static readonly WinEventDelegate activateEventDelegate;
            private static readonly WinEventDelegate resizeEventDelegate;

            private static IList<IntPtr> handles = new List<IntPtr>();

            private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            public static EventHandler<NativeEventArgs> WindowSizeOrLocationChanged = delegate { };

            static NativeMethods()
            {
                resizeEventDelegate = WinResizeEventProc;
                handles.Add(SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, resizeEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT));
            }

            [DllImport("user32.dll")]
            private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

            private static void WinResizeEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                WindowSizeOrLocationChanged(null, new NativeEventArgs(hwnd));
            }

            public static string GetWindowTitle(IntPtr hwnd)
            {
                const int nChars = 256;
                var buff = new StringBuilder(nChars);

                return GetWindowText(hwnd, buff, nChars) > 0
                    ? buff.ToString()
                    : null;
            }

            public class NativeEventArgs : EventArgs
            {
                public IntPtr Handle { get; }

                public NativeEventArgs(IntPtr hwnd)
                {
                    this.Handle = hwnd;
                }
            }
        }
    }
}