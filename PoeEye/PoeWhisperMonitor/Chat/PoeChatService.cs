using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeWhisperMonitor.Chat
{
    internal sealed class PoeChatService : DisposableReactiveObject, IPoeChatService
    {
        private readonly IKeyboardSimulator keyboardSimulator = new InputSimulator().Keyboard;

        private bool isAvailable;
        private ConcurrentQueue<PoeProcessInfo> knownProcesses = new ConcurrentQueue<PoeProcessInfo>();

        /// <summary>
        ///     The GetForegroundWindow function returns a handle to the foreground window.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public PoeChatService(
            [NotNull] IPoeTracker tracker)
        {
            Guard.ArgumentNotNull(() => tracker);

            tracker.AddTo(Anchors);

            tracker.ActiveProcesses
                .Subscribe(MakeProcessesSnapshot)
                .AddTo(Anchors);
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
            set { this.RaiseAndSetIfChanged(ref isAvailable, value); }
        }

        public PoeMessageSendStatus SendMessage(string message)
        {
            return SendMessage(message, true);
        }

        public PoeMessageSendStatus SendMessage(string message, bool terminateByPressingEnter)
        {
            Guard.ArgumentNotNull(() => message);

            PoeProcessInfo process;
            if (!knownProcesses.TryPeek(out process))
            {
                return PoeMessageSendStatus.FailedProcessNotFound;
            }

            if (process.MainWindow == IntPtr.Zero)
            {
                return PoeMessageSendStatus.FailedWindowNotFound;
            }

            try
            {
                Log.Instance.Debug(
                    $"[PoeChatService.SendMessage] Sending chat message '{message}' to process {process}...");

                SendMessageInternal(process.MainWindow, message, terminateByPressingEnter);
            }
            catch (Exception ex)
            {
                Log.Instance.Error(
                    $"[PoeChatService.SendMessage] Failed to send message '{message}' to process {process}", ex);
                return PoeMessageSendStatus.Error;
            }

            return PoeMessageSendStatus.Success;
        }

        private void MakeProcessesSnapshot(PoeProcessInfo[] processes)
        {
            knownProcesses = new ConcurrentQueue<PoeProcessInfo>(processes);
            IsAvailable = processes.Any();
        }

        private void SendMessageInternal(IntPtr hWnd, string message, bool terminateByPressingEnter)
        {
            var existingMessage = Clipboard.GetText();
            try
            {
                Clipboard.SetText(message);

                if (GetForegroundWindow() != hWnd)
                {
                    if (!SetForegroundWindow(hWnd))
                    {
                        return;
                    }
                }

                keyboardSimulator.KeyPress(VirtualKeyCode.RETURN);
                keyboardSimulator.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

                if (terminateByPressingEnter)
                {
                    keyboardSimulator.KeyPress(VirtualKeyCode.RETURN);
                }

                SetForegroundWindow(hWnd);
            }
            finally
            {
                Thread.Sleep(100);
                if (!string.IsNullOrWhiteSpace(existingMessage))
                {
                    Clipboard.SetText(existingMessage);
                }
            }
        }
    }
}