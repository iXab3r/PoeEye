﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Guards;
using JetBrains.Annotations;
using KeyboardApi;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeWhisperMonitor.Chat
{
    internal sealed class PoeChatService : DisposableReactiveObject, IPoeChatService
    {
        private bool isAvailable;
        private ConcurrentQueue<PoeProcessInfo> knownProcesses = new ConcurrentQueue<PoeProcessInfo>();

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

                SendMessageInternal(process.MainWindow, message);
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

        private void SendMessageInternal(IntPtr hWnd, string message)
        {
            Clipboard.SetText(message);

            Messaging.SendMessage(hWnd, new VKey(Messaging.VKeys.KEY_RETURN), true);
            Messaging.ForegroundKeyPressAll(hWnd, new VKey(Messaging.VKeys.KEY_V, Messaging.VKeys.KEY_CONTROL, Messaging.ShiftType.CTRL), false, true, false);
            Messaging.SendMessage(hWnd, new VKey(Messaging.VKeys.KEY_RETURN), true);
        }
    }
}