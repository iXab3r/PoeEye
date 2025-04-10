using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;

namespace PoeShared.Services;

internal sealed class TimerEx : DisposableReactiveObject, IObservable<long>
{
    private static readonly IFluentLog Log = typeof(TimerEx).PrepareLogger();

    private readonly TimeSpan period;
    private readonly bool amendPeriod;
    private readonly ISubject<long> sink = new Subject<long>();
    private readonly object padlock = new object();
    private readonly Lazy<string> toStringSupplier;
        
    private long cycleIdx;
    private Timer timer;

    public TimerEx(string timerName, TimeSpan dueTime, TimeSpan period, bool amendPeriod)
    {
        TimerName = timerName;
        toStringSupplier = new Lazy<string>(() =>
        {
            var name = string.IsNullOrEmpty(timerName) ? timerName : $"-{timerName}";
            if (dueTime == TimeSpan.Zero)
            {
                return $"Tmr{name}, {period.TotalMilliseconds:F0}ms";
            }
            else
            {
                return $"Tmr{name}, {dueTime.TotalMilliseconds:F0}ms, {period.TotalMilliseconds:F0}ms";
            }
        });
        this.period = period;
        this.amendPeriod = amendPeriod;
        timer = new Timer(Callback, null, dueTime, TimeSpan.FromMilliseconds(-1));
        Disposable.Create(() =>
        {
            lock (padlock)
            {
                timer?.Dispose();
                timer = null;
            }
        }).AddTo(Anchors);
    }
        
    public string TimerName { get; }

    private void Callback(object state)
    {
        var executionTime = TimeSpan.Zero;
        try
        {
            lock (padlock)
            {
                if (timer == null)
                {
                    return;
                }

                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            var now = Stopwatch.GetTimestamp();
            sink.OnNext(cycleIdx++);

            executionTime = TimeSpan.FromSeconds((Stopwatch.GetTimestamp() - now) / (float) Stopwatch.Frequency);
            lock (padlock)
            {
                if (timer == null)
                {
                    return;
                }

                var executeIn = TimeSpan.FromMilliseconds(
                    amendPeriod
                        ? Math.Max(0, period.TotalMilliseconds - executionTime.TotalMilliseconds)
                        : period.TotalMilliseconds);
                timer.Change(executeIn, TimeSpan.Zero);
            }
        }
        catch (Exception ex)
        {
            sink.OnError(ex);
        }
    }

    public IDisposable Subscribe(IObserver<long> observer)
    {
        return sink.Synchronize(padlock).Subscribe(observer);
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append(toStringSupplier.Value);
    }
}