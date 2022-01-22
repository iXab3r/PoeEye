using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using log4net.Appender;
using log4net.Core;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

internal sealed class ReportLastLogEventsProvider : DisposableReactiveObject, IExceptionReportItemProvider
{
    private static readonly IFluentLog Log = typeof(ReportLastLogEventsProvider).PrepareLogger();

    private readonly CircularBuffer<LoggingEvent> lastEvents = new(1000);

    public ReportLastLogEventsProvider()
    {
        var appender = new ObservableAppender();
        appender.Events
            .SubscribeSafe(
                evt => { lastEvents.PushBack(evt); }, Log.HandleUiException)
            .AddTo(Anchors);

        SharedLog.Instance.AddAppender(appender).AddTo(Anchors);
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        Log.Debug("Preparing log dump for crash report...");
        if (lastEvents.IsEmpty)
        {
            Log.Debug("Log is empty");
            yield break;
        }

        var lastLogEvents = new FileInfo(Path.Combine(outputDirectory.FullName, "logDump.log"));

        var head = lastEvents.Front();
        var tail = lastEvents.Back();
        Log.Debug(() => $"Saving log dump to {lastLogEvents} [{head.TimeStamp};{tail.TimeStamp}]");
        using (var rw = lastLogEvents.OpenWrite())
        using (var writer = new StreamWriter(rw))
        {
            foreach (var loggingEvent in lastEvents)
            {
                writer.WriteLine(loggingEvent.RenderedMessage);
            }
        }

        yield return new ExceptionReportItem()
        {
            Description = $"Last {lastEvents.Size} log records\nFirst: {head.TimeStamp}\nLast: {tail.TimeStamp}",
            Attachment = lastLogEvents
        };
    }

    private class ObservableAppender : AppenderSkeleton
    {
        private readonly ISubject<LoggingEvent> events = new Subject<LoggingEvent>();

        public IObservable<LoggingEvent> Events => events;

        protected override void Append(LoggingEvent loggingEvent)
        {
            events.OnNext(loggingEvent);
        }
    }
}