using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly IPoeChatMessageProcessor messageProcessor;
        private readonly ICollection<string> linesBuffer = new List<string>();

        private readonly ISubject<PoeMessage> messageSubject = new Subject<PoeMessage>();

        public PoeMessagesSource(
            [NotNull] FileInfo logFile,
            [NotNull] IPoeChatMessageProcessor messageProcessor)
        {
            Guard.ArgumentNotNull(logFile, nameof(logFile));
            Guard.ArgumentNotNull(messageProcessor, nameof(messageProcessor));

            this.messageProcessor = messageProcessor;
            Log.Instance.Debug($"[PoeMessagesSource] Tracking log file '{logFile.FullName}'...");
            var safeStream = new SafeFileStream(logFile.FullName);

            var linesStream = new StreamTracker(safeStream);
            linesStream.AddTo(Anchors);

            linesStream
                .Lines
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ToMessage)
                .Where(x => !default(PoeMessage).Equals(x))
                .Subscribe(messageSubject)
                .AddTo(Anchors);
        }

        public int MaxLinesBufferSize { get; set; }

        public IObservable<PoeMessage> Messages => messageSubject;

        private PoeMessage ToMessage(string rawText)
        {
            if (linesBuffer.Count >= MaxLinesBufferSize)
            {
                Log.Instance.Warn($"[PoeMessagesSource] Clearing lines buffer(maxSize: {MaxLinesBufferSize}):\r\n\t{string.Join("\r\n\t", linesBuffer)}");
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

            Log.Instance.Debug($"[PoeMessagesSource] New message: {message.DumpToText()}");
            return message;
        }
    }
}
