namespace PoeWhisperMonitor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text.RegularExpressions;

    using Chat;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;
    using PoeShared.Scaffolding;

    using TrackingStreamLib;

    internal sealed class PoeMessagesSource : DisposableReactiveObject
    {
        private readonly ICollection<string> linesBuffer = new List<string>();

        private readonly StreamTracker linesStream;
        private readonly FileInfo logFile;

        private readonly Regex logRecordRegex = new Regex(@"^(?'timestamp'\d\d\d\d\/\d\d\/\d\d \d\d:\d\d:\d\d) (?'content'.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex messageParseRegex = new Regex(
            @"^(?'timestamp'\d\d\d\d\/\d\d\/\d\d \d\d:\d\d:\d\d).*(?'prefix'[$&]|@From|@To)\s?(?:\<(?'guild'.*?)\> )?(?'name'.*):\s*(?'message'.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ISubject<PoeMessage> messageSubject = new Subject<PoeMessage>();

        public PoeMessagesSource([NotNull] FileInfo logFile)
        {
            Guard.ArgumentNotNull(() => logFile);

            this.logFile = logFile;

            Log.Instance.Debug($"[PoeMessagesSource] Tracking log file '{logFile.FullName}'...");
            var safeStream = new SafeFileStream(logFile.FullName);

            linesStream = new StreamTracker(safeStream);

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
            if (!TryParse(stringToParse, out message))
            {
                return default(PoeMessage);
            }

            linesBuffer.Clear();

            Log.Instance.Debug($"[PoeMessagesSource] New message: {message.DumpToText()}");
            return message;
        }

        private bool TryParse(string rawText, out PoeMessage message)
        {
            message = default(PoeMessage);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }


            var logRecordMatch = logRecordRegex.Match(rawText);
            if (!logRecordMatch.Success)
            {
                return false;
            }

            var match = messageParseRegex.Match(rawText);
            if (!match.Success)
            {
                // system message, error, etc
                message = new PoeMessage
                {
                    Message = logRecordMatch.Groups["content"].Value,
                    MessageType = PoeMessageType.System,
                    Timestamp = DateTime.Parse(logRecordMatch.Groups["timestamp"].Value)
                };
                return true;
            }

            message = new PoeMessage
            {
                Message = match.Groups["message"].Value,
                MessageType = ToMessageType(match.Groups["prefix"].Value),
                Name = match.Groups["name"].Value,
                Timestamp = DateTime.Parse(match.Groups["timestamp"].Value)
            };

            return true;
        }

        private PoeMessageType ToMessageType(string prefix)
        {
            switch (prefix)
            {
                case "@From":
                    return PoeMessageType.WhisperFrom;
                case "@To":
                    return PoeMessageType.WhisperTo;
                case "$":
                    return PoeMessageType.Trade;
                case "&":
                    return PoeMessageType.Guild;
                case "":
                    return PoeMessageType.Local;
                default:
                    return PoeMessageType.Unknown;
            }
        }
    }
}