using System.Reactive;
using PoeShared.Services;

namespace PoeShared.Scaffolding;

public static class Observables
{
#if NET5_0_OR_GREATER
    public static IObservable<Unit> PeriodicAsync(
        TimeSpan period,
        Func<CancellationToken, Task> supplier)
    {
        return PeriodicAsync(period, async token =>
        {
            await supplier(token);
            return Unit.Default;
        });
    }

    public static IObservable<T> PeriodicAsync<T>(
        TimeSpan period,
        Func<CancellationToken, Task<T>> supplier)
    {
        return Observable.Create<T>(async (observer, token) =>
        {
            var anchors = new CompositeDisposable();
            var periodic = new PeriodicTimer(period).AddTo(anchors);

            var initialValue = await supplier(token);
            observer.OnNext(initialValue);

            while (await periodic.WaitForNextTickAsync(token))
            {
                var value = await supplier(token);
                observer.OnNext(value);
            }

            return anchors;
        });
    }
#endif


    /// <summary>
    ///   This timer waits for callback completion before proceeding to the next tick
    /// </summary>
    /// <param name="dueTime">Initial tick offset</param>
    /// <param name="period">Tick interval, first tick will occur after offset</param>
    /// <param name="timerName"></param>
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