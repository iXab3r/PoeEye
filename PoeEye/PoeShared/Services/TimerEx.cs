using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Services
{
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
            Log.Info($"[{this}] Initializing timer");
            this.period = period;
            this.amendPeriod = amendPeriod;
            timer = new Timer(Callback, null, dueTime, TimeSpan.FromMilliseconds(-1));
            Disposable.Create(() =>
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"[{this}] Disposing - acquiring lock");
                }
                lock (padlock)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"[{this}] Disposing timer {timer}");
                    }
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
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"[{this}] Executing timer handler");
                }
               
                lock (padlock)
                {
                    if (timer == null)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"[{this}] Callback - timer is already disposed on entry");
                        }
                        return;
                    }

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"[{this}] Stopping timer loop temporarily");
                    }
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }

                var now = Stopwatch.GetTimestamp();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"[{this}] Producing OnNext");
                }
                sink.OnNext(cycleIdx++);
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"[{this}] Processed OnNext");
                }

                executionTime = TimeSpan.FromMilliseconds((Stopwatch.GetTimestamp() - now) / (float) Stopwatch.Frequency);
                lock (padlock)
                {
                    if (timer == null)
                    {
                        if (Log.IsDebugEnabled)
                        {
                            Log.Debug($"[{this}] Callback - timer is already disposed on exit");
                        }
                        return;
                    }

                    var executeIn = TimeSpan.FromMilliseconds(
                        amendPeriod
                            ? Math.Max(0, period.TotalMilliseconds - executionTime.TotalMilliseconds)
                            : period.TotalMilliseconds);
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"[{this}] Re-arming timer loop, execute in: {executeIn}");
                    }
                    timer.Change(executeIn, TimeSpan.Zero);
                }
            }
            catch (Exception ex)
            {
                if (Log.IsWarnEnabled)
                {
                    Log.Warn($"[{this}] Timer handler captured an error, propagating to sink", ex);
                }
                sink.OnError(ex);
            }
            finally
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"[{this}] Timer handler completed in {executionTime.TotalMilliseconds:F0}ms");
                }
            }
        }

        public IDisposable Subscribe(IObserver<long> observer)
        {
            return sink.Synchronize(padlock).Subscribe(observer);
        }

        public override string ToString()
        {
            return toStringSupplier.Value;
        }
    }
}