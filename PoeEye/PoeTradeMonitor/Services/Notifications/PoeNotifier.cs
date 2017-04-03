using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Guards;
using JetBrains.Annotations;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using PoeEye.TradeMonitor.Modularity;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Services.Notifications
{
    internal sealed class PoeNotifier : DisposableReactiveObject, IPoeNotifier
    {
        [NotNull] private readonly IClock clock;
        private readonly IConfigProvider<PoeTradeMonitorConfig> configProvider;
        private static readonly TimeSpan BufferPeriod = TimeSpan.FromSeconds(10);

        private readonly ISubject<string> messagesQueue = new Subject<string>();

        public PoeNotifier(
            [NotNull] ISchedulerProvider schedulerProvider,
            [NotNull] IClock clock,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider)
        {
            Guard.ArgumentNotNull(schedulerProvider, nameof(schedulerProvider));
            Guard.ArgumentNotNull(clock, nameof(clock));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            this.clock = clock;
            this.configProvider = configProvider;

            var bgScheduler = schedulerProvider.GetOrCreate("Notifications");
            messagesQueue
                .ObserveOn(bgScheduler)
                .Buffer(BufferPeriod)
                .Select(x => x.ToArray())
                .Where(x => x.Length > 0)
                .Subscribe(HandleMessageRequest, Log.HandleException)
                .AddTo(Anchors);
        }

        public void SendNotification(string textMessage, NotificationLevel level)
        {
            Guard.ArgumentNotNull(textMessage, nameof(textMessage));

            messagesQueue.OnNext(textMessage);
        }

        private void HandleMessageRequest(string[] messagesToSend)
        {
            try
            {
                var emailAddress = configProvider.ActualConfig.CriticalNotificationEmailAddress;
                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    return;
                }

                Log.Instance.Debug($"[Poe.MailNotifier] Preparing message to '{emailAddress}'");
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("PoeEye", "mail.poeeye@gmail.com"));
                message.To.Add(new MailboxAddress(Environment.UserName, emailAddress));
                message.Subject = $"PoeEye notifications - {messagesToSend.Length} item(s)";

                message.Body = new TextPart("plain")
                {
                    Text = $"[PoeEye]\n{messagesToSend.DumpToText()}",
                };
                Log.Instance.Debug($"[Poe.MailNotifier] Message body:\n{message.Body}");

                var logger = new EmailLogger(clock);
                Log.Instance.Debug($"[Poe.MailNotifier] Sending message...");
                using (var client = new SmtpClient(logger))
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect("aspmx.l.google.com", 25, false);

                    client.Send(message);
                    client.Disconnect(true);
                }

                var loggerLog = string.Join("\n\t", logger);
                Log.Instance.Debug($"[Poe.MailNotifier] Send message log:\n\t{loggerLog}");
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }
        }

        private sealed class EmailLogger : IProtocolLogger, IEnumerable<string>
        {
            private readonly IClock clock;
            private ConcurrentQueue<string> log = new ConcurrentQueue<string>();

            public EmailLogger(IClock clock)
            {
                this.clock = clock;
                LogMessage("Created");
            }

            private void LogMessage(string message)
            {
                log.Enqueue($"[{clock.Now}] {message.Trim('\n', '\r', ' ', '\t')}");
            }

            public void Dispose()
            {
                LogMessage("Disposed");
            }

            public void LogConnect(Uri uri)
            {
                LogMessage($"Connect: {uri}");
            }

            public void LogClient(byte[] buffer, int offset, int count)
            {
                LogInteraction("Client", buffer, offset, count);
            }

            public void LogServer(byte[] buffer, int offset, int count)
            {
                LogInteraction("Server", buffer, offset, count);
            }

            private void LogInteraction(string tag, byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    LogMessage($"[{tag}] Invalid buffer - null");
                    return;
                }

                if (buffer.Length < offset || buffer.Length < offset + count)
                {
                    LogMessage($"[{tag}] Invalid buffer - out of bound, buffer.length: {buffer.Length}, offset: {offset}, count: {count}");
                    return;
                }

                try
                {
                    var msg = Encoding.Default.GetString(buffer, offset, count);
                    LogMessage($"[{tag}] {msg}");
                }
                catch (Exception e)
                {
                    LogMessage($"[{tag}] Failed to decode message: {e}\nBuffer: {buffer.DumpToText()}\nOffset: {offset}\nCount: {count}");
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<string> GetEnumerator()
            {
                return log.GetEnumerator();
            }
        }
    }
}
