using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class TimerEx : DisposableReactiveObject, IObservable<long>
    {
        private readonly TimeSpan period;
        private readonly ISubject<long> sink = new Subject<long>();
        private readonly object padlock = new object();
        
        private long cycleIdx;
        private Timer timer;

        public TimerEx(TimeSpan dueTime, TimeSpan period)
        {
            this.period = period;
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
            
            sink.OnNext(cycleIdx++);
            
            lock (padlock)
            {
                if (timer == null)
                {
                    return;
                }
                timer.Change(period, TimeSpan.Zero);
            }
        }

        public IDisposable Subscribe(IObserver<long> observer)
        {
            return sink.Subscribe(observer);
        }
    }
}