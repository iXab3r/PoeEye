using PoeShared.Services;

namespace PoeShared.Scaffolding;

public static class ObservableEx
{
    /// <summary>
    ///   This timer waits for callback completion before proceeding to the next tick
    /// </summary>
    /// <param name="dueTime">Initial tick offset</param>
    /// <param name="period">Tick interval, first tick will occur after offset</param>
    /// <param name="amendPeriod"></param>
    /// <returns></returns>
    public static IObservable<long> BlockingTimer(TimeSpan period, string timerName = null, bool? amendPeriod = null)
    {
        return Observable.Create<long>(observer =>
        {
            var anchors = new CompositeDisposable();
            var serviceTimer = new TimerEx(timerName, TimeSpan.Zero, period, amendPeriod ?? false).AddTo(anchors);
            serviceTimer.Subscribe(observer).AddTo(anchors);
            return anchors;
        });
    }
}