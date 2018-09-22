using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Common.Logging;
using Guards;
using JetBrains.Annotations;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeWhisperMonitor.Chat;

namespace PoeWhisperMonitor
{
    internal sealed class PoeWhisperService : DisposableReactiveObject, IPoeWhisperService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeWhisperService));

        private readonly IFactory<IPoeMessagesSource, FileInfo> messagesSourceFactory;
        private readonly ISubject<PoeMessage> messagesSubject = new Subject<PoeMessage>();
        private readonly IDictionary<string, IDisposable> sourcesByPath = new Dictionary<string, IDisposable>();

        public PoeWhisperService(
            [NotNull] IPoeTracker tracker,
            [NotNull] IFactory<IPoeMessagesSource, FileInfo> messagesSourceFactory)
        {
            Guard.ArgumentNotNull(tracker, nameof(tracker));
            Guard.ArgumentNotNull(messagesSourceFactory, nameof(messagesSourceFactory));

            this.messagesSourceFactory = messagesSourceFactory;
            var converter = new PoeProcessToLogFilePathConverter();

            tracker.AddTo(Anchors);

            tracker.ActiveProcesses
                   .Select(x => x.Select(converter.Convert).Select(y => y.FullName).ToArray())
                   .Subscribe(ProcessLogFile)
                   .AddTo(Anchors);
        }

        public IObservable<PoeMessage> Messages => messagesSubject;

        private void ProcessLogFile(string[] logFiles)
        {
            var sourcesToRemove = sourcesByPath.Keys.Except(logFiles).ToArray();
            var sourcesToAdd = logFiles.Except(sourcesByPath.Keys).ToArray();

            if (!sourcesToAdd.Any() && !sourcesToRemove.Any())
            {
                return;
            }

            Log.Debug(
                $"[PoeWhispers] Log files list changed:\r\n\tLogs to add: {string.Join(", ", sourcesToAdd)}\r\n\tLogs to remove: {string.Join(", ", sourcesToRemove)}");

            foreach (var logFilePath in sourcesToRemove)
            {
                var source = sourcesByPath[logFilePath];
                sourcesByPath.Remove(logFilePath);

                source.Dispose();
            }

            foreach (var logFilePath in sourcesToAdd)
            {
                var source = messagesSourceFactory.Create(new FileInfo(logFilePath));

                var composite = new CompositeDisposable {source};
                source.Messages.Subscribe(messagesSubject).AddTo(composite);

                sourcesByPath.Add(logFilePath, composite);
            }
        }
    }
}