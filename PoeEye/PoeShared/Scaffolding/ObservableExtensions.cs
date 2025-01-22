using System.Reactive;
using System.Reactive.Concurrency;
using DynamicData.Kernel;
using JetBrains.Annotations;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.Scaffolding;

public static class ObservableExtensions
{
    private static Func<TimeSpan, int, TimeSpan> DefaultDelayStrategy => (retryTimeout, attemptIdx) => attemptIdx switch
    {
        < 1 => retryTimeout / 30,
        < 2 => retryTimeout / 6,
        < 4 => retryTimeout / 3,
        < 5 => retryTimeout / 2,
        _ => retryTimeout
    };

    private static readonly Action NoOperation = () => { };
    
    public static IObservable<TResult> SwitchLatestAsync<TSource, TResult>(this IObservable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> asyncMethod)
    {
        return Observable.Create<TResult>(async observer =>
        {
            var cts = new CancellationTokenSource();
            try
            {
                // ReSharper disable once MethodSupportsCancellation reason: By design - cts should cancel only inner loop
                await source.ForEachAsync(async item =>
                {
                    cts.Cancel(); // cancel old request
                    cts = new CancellationTokenSource();
                    try
                    {
                        var result = await asyncMethod(item, cts.Token);
                        observer.OnNext(result);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when the operation is cancelled, we don't have to do anything special here.
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when the operation is cancelled, we don't have to do anything special here.
                    }
                });
            }
            catch (Exception ex)
            {
                observer.OnError(ex); // Propagate the error to the observer
            }

            observer.OnCompleted();
        });
    }

    public static IObservable<T> Synchronize<T>(this IObservable<T> observable, NamedLock gate)
    {
        return observable.Synchronize(gate.Gate);
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<Task> supplier)
    {
        return SubscribeAsync(observable, _ => supplier());
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> supplier)
    {
        return SubscribeAsync(observable, supplier, _ => { });
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<Task> supplier, Action<Exception> onError)
    {
        return SubscribeAsync(observable, _ => supplier(), onError);
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, CancellationToken, Task> supplier)
    {
        return SubscribeAsync(observable, supplier, _ => { });
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, CancellationToken, Task> supplier, Action<Exception> onError)
    {
        return observable.Select(x => Observables.FromAsyncSafe(token => supplier(x, token)).Take(1)).Concat().Subscribe(() => { }, onError);
    }

    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> supplier, Action<Exception> onError)
    {
        return observable.Select(x => Observables.FromAsyncSafe(_ => supplier(x)).Take(1)).Concat().Subscribe(() => { }, onError);
    }

    public static IObservable<T1> SelectAsync<T, T1>(this IObservable<T> observable, Func<T, CancellationToken, Task<T1>> supplier)
    {
        return observable.Select(x => Observables.FromAsyncSafe((token) => supplier(x, token)).Take(1)).Concat();
    }

    public static IObservable<T1> SelectAsync<T, T1>(this IObservable<T> observable, Func<T, Task<T1>> supplier)
    {
        return observable.Select(x => Observables.FromAsyncSafe(_ => supplier(x)).Take(1)).Concat();
    }

    public static IObservable<T1> SelectAsync<T, T1>(this IObservable<T> observable, Func<T, Task<T1>> supplier, IScheduler scheduler)
    {
        return observable.Select(x => Observables.FromAsyncSafe(_ => supplier(x)).Take(1)).Concat();
    }

    public static IObservable<Unit> SelectAsync<T>(this IObservable<T> observable, Func<T, Task> supplier, IScheduler scheduler)
    {
        return observable.Select(x => Observables.FromAsyncSafe(_ => supplier(x)).Take(1)).Concat();
    }

    public static IObservable<T> RepeatWithBackoff<T>(
        this IObservable<T> observable,
        TimeSpan repeatTimeout)
    {
        return observable
            .RepeatWithBackoff(attemptIdx => DefaultDelayStrategy(repeatTimeout, attemptIdx));
    }

    public static IObservable<T> RepeatWithBackoff<T>(
        this IObservable<T> observable,
        Func<int, TimeSpan?> strategy)
    {
        var attemptIdx = 0;
        var pipeline = Observable
            .Defer(
                () =>
                {
                    if (attemptIdx++ == 0)
                    {
                        return observable;
                    }

                    var delay = strategy(attemptIdx);
                    return delay == null
                        ? Observable.Empty<T>()
                        : observable.DelaySubscription(delay.Value);
                });

        return pipeline.Repeat();
    }

    public static IObservable<T> RetryWithBackOff<T>(
        this IObservable<T> observable,
        Func<Exception, int, TimeSpan?> strategy)
    {
        return observable
            .RetryWithBackOff<T, Exception>(strategy);
    }

    public static IObservable<T> RetryWithBackOff<T>(
        this IObservable<T> observable,
        TimeSpan retryTimeout)
    {
        return observable
            .RetryWithBackOff<T, Exception>((_, attemptIdx) => DefaultDelayStrategy(retryTimeout, attemptIdx));
    }

    [DebuggerStepThrough]
    public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action onNext)
    {
        return observable.Subscribe(_ => onNext());
    }

    [DebuggerStepThrough]
    public static IDisposable SubscribeToErrors<T>(this IObservable<T> source, Action<Exception> onError)
    {
        return SubscribeSafe(source, x => { }, onError);
    }

    [DebuggerStepThrough]
    public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action onNext, Action<Exception> onError)
    {
        return SubscribeSafe(source, x => onNext(), onError);
    }

    [DebuggerStepThrough]
    public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError)
    {
        return SubscribeSafe(source, onNext, onError, NoOperation);
    }

    [DebuggerStepThrough]
    public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (onNext == null) throw new ArgumentNullException(nameof(onNext));
        if (onError == null) throw new ArgumentNullException(nameof(onError));
        if (onCompleted == null) throw new ArgumentNullException(nameof(onCompleted));

        return source.Subscribe([DebuggerStepThrough](x) =>
        {
            try
            {
                onNext(x);
            }
            catch (Exception e)
            {
                onError(e);
            }
        }, onError, () =>
        {
            try
            {
                onCompleted();
            }
            catch (Exception e)
            {
                onError(e);
            }
        });
    }

    public static IDisposable SubscribeSafe<T>(this IObservable<T> source,
        Func<Task> asyncAction, Action<Exception> handler)
    {
        async Task<Unit> Wrapped(T t)
        {
            try
            {
                await asyncAction();
            }
            catch (Exception e)
            {
                handler(e);
            }

            return Unit.Default;
        }

        return source.SelectMany(Wrapped).SubscribeSafe(_ => { }, handler);
    }

    public static IDisposable SubscribeSafe<T>(this IObservable<T> source,
        Func<T, Task> asyncAction, Action<Exception> handler)
    {
        async Task<Unit> Wrapped(T t)
        {
            try
            {
                await asyncAction(t);
            }
            catch (Exception e)
            {
                handler(e);
            }

            return Unit.Default;
        }

        return source.SelectMany(Wrapped).SubscribeSafe(_ => { }, handler);
    }

    public static IObservable<TOut> SwitchIfNotDefault<TIn, TOut>(
        this IObservable<TIn> observable,
        Func<TIn, IObservable<TOut>> trueSelector,
        Func<IObservable<TOut>> falseSelector)
    {
        return observable.SwitchIf(condition: x => x != null, trueSelector: trueSelector, falseSelector: _ => falseSelector());
    }

    public static IObservable<TOut> SwitchIfNotDefault<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, IObservable<TOut>> selector)
    {
        return SwitchIfNotDefault(observable, trueSelector: selector, falseSelector: Observable.Empty<TOut>);
    }

    public static IObservable<T> EnableIf<T>(this IObservable<T> source, IObservable<bool> condition)
    {
        return EnableIf(source, condition, Observable.Empty<T>());
    }

    public static IObservable<T> EnableIf<T>(
        this IObservable<T> source,
        IObservable<bool> condition,
        IObservable<T> alternateSource)
    {
        return condition
            .Select(x => x ? source : alternateSource)
            .Switch();
    }

    public static IObservable<TOut> SwitchIf<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservable<TOut>> trueSelector)
    {
        return observable.SwitchIf(condition, trueSelector, _ => Observable.Empty<TOut>());
    }

    public static IObservable<TOut> SwitchIf<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Predicate<TIn> condition,
        [NotNull] Func<TIn, IObservable<TOut>> trueSelector,
        [NotNull] Func<TIn, IObservable<TOut>> falseSelector)
    {
        return observable
            .Select(x => condition(x) ? trueSelector(x) : falseSelector(x))
            .Switch();
    }

    public static IObservable<TOut> SelectSafeOrDefault<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, TOut> onNext)
    {
        return observable.SelectSafeOrDefault(onNext, _ => { });
    }

    public static IObservable<TOut> SelectSafeOrDefault<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, TOut> onNext,
        [NotNull] Action<Exception> onError)
    {
        return observable.SelectSafe<TIn, TOut, Exception>(onNext, ex =>
        {
            onError(ex);
            return default;
        });
    }

    public static IObservable<TOut> SelectSafe<TIn, TOut, TException>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, TOut> onNext,
        [NotNull] Func<TException, TOut> onError)
        where TException : Exception
    {
        return observable.Select(input =>
        {
            try
            {
                return onNext(input);
            }
            catch (TException e)
            {
                return onError(e);
            }
        });
    }

    public static IObservable<TOut> SelectSafe<TIn, TOut>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, TOut> onNext,
        [NotNull] Func<TIn, Exception, TOut> onError)
    {
        return SelectSafe<TIn, TOut, Exception>(observable, onNext, onError);
    }

    public static IObservable<TOut> SelectSafe<TIn, TOut, TException>(
        this IObservable<TIn> observable,
        [NotNull] Func<TIn, TOut> onNext,
        [NotNull] Func<TIn, TException, TOut> onError)
        where TException : Exception
    {
        return observable.Select(input =>
        {
            try
            {
                return onNext(input);
            }
            catch (TException e)
            {
                return onError(input, e);
            }
        });
    }

    public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action onNext, [NotNull] Action<Exception> onError)
    {
        return observable.Subscribe(_ => onNext(), onError);
    }

    public static IObservable<Unit> ToUnit<T>(this IObservable<T> observable)
    {
        return observable.Select(_ => Unit.Default);
    }

    public static IObservable<T> StartWithDefault<T>(this IObservable<T> observable)
    {
        return observable.StartWith(default(T));
    }

    public static IObservable<Y> SelectTo<T, Y>(this IObservable<T> observable, Func<Y> selector)
    {
        return observable.Select(_ => selector());
    }

    public static IObservable<TResult> WithPrevious<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, TSource, TResult> resultSelector)
    {
        return WithPrevious(source)
            .Select(t => resultSelector(t.Item1, t.Item2));
    }

    public static IObservable<(TSource Previous, TSource Current)> WithPrevious<TSource>(
        this IObservable<TSource> source)
    {
        return source.Scan(
            (default(TSource), default(TSource)),
            (previous, current) => (previous.Item2, current));
    }

    public static IObservable<TSource> SkipUntil<TSource>(
        this IObservable<TSource> source,
        Func<TSource, bool> condition)
    {
        var sharedObservable = source.Publish().RefCount();
        return sharedObservable.SkipWhile(condition).Take(1).Concat(sharedObservable);
    }

    public static IObservable<TSource> DoWithPrevious<TSource>(
        this IObservable<TSource> source, Action<TSource> action)
    {
        return WithPrevious(source).Select(x =>
        {
            action(x.Item1);
            return x.Item2;
        });
    }

    public static IObservable<TSource> DisposePrevious<TSource>(
        this IObservable<TSource> source) where TSource : IDisposable
    {
        return source.DoWithPrevious(x => x?.Dispose());
    }

    public static IObservable<T> RetryWithDelay<T>(this IObservable<T> source, TimeSpan timeSpan)
    {
        return RetryWithDelay(source, timeSpan, Scheduler.Default);
    }

    public static IObservable<T> RetryWithDelay<T>(this IObservable<T> source, TimeSpan timeSpan, IScheduler scheduler)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException("timeSpan");
        }

        if (timeSpan == TimeSpan.Zero)
        {
            return source.Retry();
        }

        return source.Catch(source.SubscribeOn(scheduler).DelaySubscription(timeSpan).Retry());
    }

    public static ObservableAsPropertyHelper<TSourceProperty> ToProperty<TSourceProperty>(
        [NotNull] this IObservable<TSourceProperty> sourceObservable)
    {
        var result = new ObservableAsPropertyHelper<TSourceProperty>(sourceObservable, onChanged: src => { }, onChanging: null, initialValue: default, deferSubscription: false);
        return result;
    }

    public static ObservableAsPropertyHelper<TSourceProperty> ToProperty<TSource, TSourceProperty>(
        [NotNull] this IObservable<TSourceProperty> sourceObservable,
        out ObservableAsPropertyHelper<TSourceProperty> result,
        [NotNull] TSource instance,
        [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
        [CanBeNull] IScheduler scheduler = null)
        where TSource : IDisposableReactiveObject
    {
        result = instance.ToProperty(instancePropertyExtractor, sourceObservable, default, false, scheduler);
        return result;
    }

    public static ObservableAsPropertyHelper<TSourceProperty> ToProperty<TSource, TSourceProperty>(
        [NotNull] this IObservable<TSourceProperty> sourceObservable,
        [NotNull] TSource instance,
        [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
        [CanBeNull] IScheduler scheduler = null)
        where TSource : IDisposableReactiveObject
    {
        return instance.ToProperty(instancePropertyExtractor, sourceObservable, default, false, scheduler);
    }

    /// <summary>
    /// https://www.zerobugbuild.com/?p=192
    /// This is a problem that comes up when the UI dispatcher can’t keep up with inbound activity.
    /// the example I saw on a project was a price stream that had to be displayed on the UI that could get very busy.
    /// If more than one new price arrives on background threads in between dispatcher time slices, there is no point displaying anything but the most recent price – in fact we want the UI to “catch up” by dropping all but the most recent undisplayed price.
    /// So the operation is like an ObserveOn that drops all but the most recent events. Here’s a picture of what’s happening – notice how price 2 is dropped and how prices are only published during the dispatcher time slice:
    /// The key idea here is that we keep track of the notification to be pushed on to the target scheduler in pendingNotification – and whenever an event is received, we swap the pendingNotification for the new notification. We ensure the new notification will be scheduled for dispatch on the target scheduler – but we may not need to do this…
    /// If the previousNotification is null we know that either (a) there was no previous notification as this is the first one or (b) the previousNotification was already dispatched. How to we know this? Because in the scheduler action that does the dispatch we swap the pendingNotification for null! So if previousNotification is null, we know we must schedule a new dispatch action.
    /// This approach keeps the checks, locks and scheduled actions to a minimum.
    /// Notes and credits:
    /// I’ve gone round the houses a few times on this implementation – my own attempts to improve it to use CAS rather than lock ran into bugs, so the code below is largely due to Lee Campbell, and edited for RX 2.0 support by Wilka Hudson. For an interesting discussion on this approach see this thread on the official RX forum.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="scheduler"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IObservable<T> ObserveLatestOn<T>(this IObservable<T> source, IScheduler scheduler)
    {
        return Observable.Create<T>(observer =>
        {
            Notification<T> outsideNotification;
            var gate = new object();
            var active = false;
            var cancelable = new MultipleAssignmentDisposable();
            var disposable = source.Materialize().Subscribe(thisNotification =>
            {
                bool wasNotAlreadyActive;
                lock (gate)
                {
                    wasNotAlreadyActive = !active;
                    active = true;
                    outsideNotification = thisNotification;
                }

                if (wasNotAlreadyActive)
                {
                    cancelable.Disposable = scheduler.Schedule(self =>
                    {
                        Notification<T> localNotification;
                        lock (gate)
                        {
                            localNotification = outsideNotification;
                            outsideNotification = null;
                        }

                        localNotification.Accept(observer);
                        bool hasPendingNotification;
                        lock (gate)
                        {
                            hasPendingNotification = active = (outsideNotification != null);
                        }

                        if (hasPendingNotification)
                        {
                            self();
                        }
                    });
                }
            });
            return new CompositeDisposable(disposable, cancelable);
        });
    }

    /// <summary>
    /// Allows to attach TTL to each value of the stream. If next value is not produced in expected time slice, fallback value is sent into stream as replacement until next value is propagated.
    /// </summary>
    /// <returns></returns>
    public static IObservable<T> WithExpirationTime<T>(
        this IObservable<T> source,
        TimeSpan expirationTime,
        Func<T, T> fallbackValueSupplier)
    {
        if (expirationTime <= TimeSpan.Zero)
        {
            return source;
        }

        return source
            .Select(x => Observable.Return(x).Concat(Observable.Timer(expirationTime).Select(_ => fallbackValueSupplier(x)).Take(1))).Switch();
    }
}