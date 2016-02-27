namespace PoeWhisperMonitor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using Chat;

    using PoeShared;
    using PoeShared.Scaffolding;

    internal sealed class PoeWhispers : DisposableReactiveObject, IPoeWhispers
    {
        private readonly ISubject<PoeMessage> messagesSubject = new Subject<PoeMessage>();
        private readonly IDictionary<string, IDisposable> sourcesByPath = new Dictionary<string, IDisposable>();

        public PoeWhispers()
        {
            var tracker = new PoeTracker();
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

            Log.Instance.Debug(
                $"[PoeWhispers] Log files list changed:\r\n\tLogs to add: {string.Join(", ", sourcesToAdd)}\r\n\tLogs to remove: {string.Join(", ", sourcesToRemove)}");

            foreach (var logFilePath in sourcesToRemove)
            {
                var source = sourcesByPath[logFilePath];
                sourcesByPath.Remove(logFilePath);

                source.Dispose();
            }

            foreach (var logFilePath in sourcesToAdd)
            {
                var source = new PoeMessagesSource(new FileInfo(logFilePath));

                var composite = new CompositeDisposable {source};
                source.Messages.Subscribe(messagesSubject).AddTo(composite);

                sourcesByPath.Add(logFilePath, composite);
            }
        }
    }
}