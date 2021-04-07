using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class TimerEx : DisposableReactiveObject, IObservable<long>
    {
        private readonly TimeSpan period;
        private readonly bool amendPeriod;
        private readonly ISubject<long> sink = new Subject<long>();
        private readonly object padlock = new object();
        
        private long cycleIdx;
        private Timer timer;

        public TimerEx(TimeSpan dueTime, TimeSpan period, bool amendPeriod)
        {
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

        private void Callback(object state)
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
            var executionTime = TimeSpan.FromMilliseconds((Stopwatch.GetTimestamp() - now) / (float) Stopwatch.Frequency);

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

        public IDisposable Subscribe(IObserver<long> observer)
        {
            return sink.Synchronize().Subscribe(observer);
        }
    }
}