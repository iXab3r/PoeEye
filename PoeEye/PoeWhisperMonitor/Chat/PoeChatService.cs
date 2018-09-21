﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeWhisperMonitor.Chat
{
    internal sealed class PoeChatService : DisposableReactiveObject, IPoeChatService
    {
        private static readonly ILog Log = LogManager.GetLogger<PoeChatService>();
        
        private readonly IClipboardManager clipboardManager;

        private readonly IKeyboardSimulator keyboardSimulator = new InputSimulator().Keyboard;
        private readonly ISubject<MessageRequest> requests = new Subject<MessageRequest>();

        private bool isAvailable;

        private bool isBusy;
        private ConcurrentQueue<PoeProcessInfo> knownProcesses = new ConcurrentQueue<PoeProcessInfo>();

        public PoeChatService(
            [NotNull] IPoeTracker tracker,
            [NotNull] IClipboardManager clipboardManager,
            [NotNull] ISchedulerProvider schedulerProvider)
        {
            this.clipboardManager = clipboardManager;
            Guard.ArgumentNotNull(clipboardManager, nameof(clipboardManager));
            Guard.ArgumentNotNull(tracker, nameof(tracker));

            tracker.AddTo(Anchors);

            tracker.ActiveProcesses
                   .Subscribe(MakeProcessesSnapshot)
                   .AddTo(Anchors);

            var kbdScheduler = schedulerProvider.GetOrCreate("KbdOutput");
            requests
                .ObserveOn(kbdScheduler)
                .Subscribe(HandleMessageRequest, Log.HandleException)
                .AddTo(Anchors);
        }

        public bool IsAvailable
        {
            get => isAvailable;
            set => this.RaiseAndSetIfChanged(ref isAvailable, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => this.RaiseAndSetIfChanged(ref isBusy, value);
        }

        public Task<PoeMessageSendStatus> SendMessage(string message)
        {
            return SendMessage(message, true);
        }

        public async Task<PoeMessageSendStatus> SendMessage(string message, bool terminateByPressingEnter)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            try
            {
                IsBusy = true;

                Log.Debug(
                    $"[PoeChatService.SendMessage] Sending chat message '{message}'...");

                var consumer = new TaskCompletionSource<PoeMessageSendStatus>();
                requests.OnNext(new MessageRequest(message, terminateByPressingEnter, consumer));

                return await consumer.Task;
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"[PoeChatService.SendMessage] Failed to send message '{message}'", ex);
                return PoeMessageSendStatus.Error;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void MakeProcessesSnapshot(PoeProcessInfo[] processes)
        {
            knownProcesses = new ConcurrentQueue<PoeProcessInfo>(processes);
            IsAvailable = processes.Any();
        }

        private void HandleMessageRequest(MessageRequest request)
        {
            try
            {
                PoeProcessInfo process;
                if (!knownProcesses.TryPeek(out process))
                {
                    Log.Trace($"[PoeChatService] Failed to get active process");
                    request.Consumer.TrySetResult(PoeMessageSendStatus.FailedProcessNotFound);
                    return;
                }

                var poeHwnd = process.MainWindow;
                Log.Trace($"[PoeChatService] [{process}] MainWindow: {poeHwnd.ToInt64():x8}");
                if (poeHwnd == IntPtr.Zero)
                {
                    request.Consumer.TrySetResult(PoeMessageSendStatus.FailedWindowNotFound);
                    return;
                }

                Log.Trace($"[PoeChatService] [{process}] Bringing window {poeHwnd.ToInt64():x8} to front...");
                if (NativeMethods.GetForegroundWindow() != poeHwnd)
                {
                    if (!NativeMethods.SetForegroundWindow(poeHwnd))
                    {
                        request.Consumer.TrySetResult(PoeMessageSendStatus.FailedToActivateWindow);
                        return;
                    }
                }

                Log.Trace($"[PoeChatService] [{process}] Retrieving current clipboard content...");
                var existingClipboardContent = clipboardManager.GetDataObject();

                Log.Trace($"[PoeChatService] [{process}] Setting new message '{request.Message}'...");
                clipboardManager.SetText(request.Message);

                keyboardSimulator.KeyPress(VirtualKeyCode.RETURN);
                keyboardSimulator.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);

                if (request.TerminateByEnter)
                {
                    keyboardSimulator.KeyPress(VirtualKeyCode.RETURN);
                }

                if (existingClipboardContent != null)
                {
                    Thread.Sleep(clipboardManager.ClipboardRestorationTimeout);
                    Log.Trace($"[PoeChatService] Restoring previous clipboard content");
                    clipboardManager.SetDataObject(existingClipboardContent);
                }

                Log.Trace($"[PoeChatService] [{process}] Successfully sent message '{request.Message}'");
                request.Consumer.TrySetResult(PoeMessageSendStatus.Success);
            }
            catch (Exception ex)
            {
                request.Consumer.TrySetException(ex);
            }
        }

        private struct MessageRequest
        {
            public string Message { get; }

            public bool TerminateByEnter { get; }

            public TaskCompletionSource<PoeMessageSendStatus> Consumer { get; }

            public MessageRequest(string message, bool terminateByEnter, TaskCompletionSource<PoeMessageSendStatus> consumer) : this()
            {
                Message = message;
                TerminateByEnter = terminateByEnter;
                Consumer = consumer;
            }
        }
    }
}