using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;
using TrackingStreamLib;

namespace PoeWhisperMonitor
{
    internal sealed class PoeMessagesSource : DisposableReactiveObject, IPoeMessagesSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeMessagesSource));
        
        private readonly ICollection<string> linesBuffer = new List<string>();
        private readonly IPoeChatMessageProcessor messageProcessor;

        private readonly ISubject<PoeMessage> messageSubject = new Subject<PoeMessage>();

        public PoeMessagesSource(
            [NotNull] FileInfo logFile,
            [NotNull] IPoeChatMessageProcessor messageProcessor)
        {
            Guard.ArgumentNotNull(logFile, nameof(logFile));
            Guard.ArgumentNotNull(messageProcessor, nameof(messageProcessor));

            this.messageProcessor = messageProcessor;
            Log.Debug($"[PoeMessagesSource] Tracking log file '{logFile.FullName}'...");
            var safeStream = new SafeFileStream(logFile.FullName);

            var linesStream = new StreamTracker(safeStream);
            linesStream.AddTo(Anchors);

            linesStream
                .Lines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Where(x => MaxLinesBufferSize > 0)
                .Select(ToMessage)
                .Where(x => !default(PoeMessage).Equals(x))
                .Subscribe(messageSubject)
                .AddTo(Anchors);
        }

        public int MaxLinesBufferSize { get; set; } = 1024;

        public IObservable<PoeMessage> Messages => messageSubject;

        private PoeMessage ToMessage(string rawText)
        {
            if (linesBuffer.Count >= MaxLinesBufferSize)
            {
                Log.Trace($"[PoeMessagesSource] Clearing lines buffer(maxSize: {MaxLinesBufferSize}):\r\n\t{string.Join("\r\n\t", linesBuffer)}");
                linesBuffer.Clear();
            }

            linesBuffer.Add(rawText);

            var stringToParse = string.Join(string.Empty, linesBuffer);

            PoeMessage message;
            if (!messageProcessor.TryParse(stringToParse, out message))
            {
                return default(PoeMessage);
            }

            linesBuffer.Clear();

            Log.Debug($"[PoeMessagesSource] New message: {message.DumpToTextRaw()}");
            return message;
        }
    }
}