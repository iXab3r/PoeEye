using System.Reactive.Subjects;
using log4net.Appender;
using log4net.Core;

namespace PoeShared.Logging;

public sealed class ObservableAppender : AppenderSkeleton
{
    private readonly ISubject<LoggingEvent> events = new Subject<LoggingEvent>();

    public IObservable<LoggingEvent> Events => events;

    protected override void Append(LoggingEvent loggingEvent)
    {
        events.OnNext(loggingEvent);
    }
}