using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using PoeShared.Services;

namespace PoeShared.Scaffolding
{
    public static class ObservableEx
    {
        /// <summary>
        ///   This timer waits for callback completion before proceeding to the next tick
        /// </summary>
        /// <param name="interval">Tick interval, first tick will occur instantly</param>
        /// <returns></returns>
        public static IObservable<long> BlockingTimer(TimeSpan interval)
        {
            return BlockingTimer(TimeSpan.Zero, interval);
        }

        public static IObservable<long> BlockingTimer(TimeSpan dueTime, TimeSpan period)
        {
            return BlockingTimer(dueTime, period, false);
        }

        /// <summary>
        ///   This timer waits for callback completion before proceeding to the next tick
        /// </summary>
        /// <param name="dueTime">Initial tick offset</param>
        /// <param name="period">Tick interval, first tick will occur after offset</param>
        /// <param name="amendPeriod"></param>
        /// <returns></returns>
        public static IObservable<long> BlockingTimer(TimeSpan dueTime, TimeSpan period, bool amendPeriod)
        {
            return Observable.Create<long>(observer =>
            {
                var anchors = new CompositeDisposable();
                var serviceTimer = new TimerEx(dueTime, period, amendPeriod).AddTo(anchors);
                serviceTimer.Subscribe(observer).AddTo(anchors);
                return anchors;
            });
        }
    }
}