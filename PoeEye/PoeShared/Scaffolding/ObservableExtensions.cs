using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public static class ObservableExtensions
    {
        private static readonly Action NoOperation = () => { };

        public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action onNext)
        {
            return observable.Subscribe(_ => onNext());
        }
        
        public static IDisposable SubscribeToErrors<T>(this IObservable<T> source, Action<Exception> onError)
        {
            return SubscribeSafe(source, x => { }, onError);
        }

        public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action onNext, Action<Exception> onError)
        {
            return SubscribeSafe(source, x => onNext(), onError);
        }
        
        public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError)
        {
            return SubscribeSafe(source, onNext, onError, NoOperation);
        }
        
        public static IDisposable SubscribeSafe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            if (onError == null) throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null) throw new ArgumentNullException(nameof(onCompleted));

            return source.Subscribe(x =>
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

        public static IObservable<TOut> SelectSafeOrDefault<TIn, TOut>(
            this IObservable<TIn> observable,
            [NotNull] Func<TIn, TOut> onNext)
        {
            return observable.SelectSafe<TIn, TOut, Exception>(onNext, ex => default);
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
        
        public static IObservable<TSource> DisposePrevious<TSource>(
            this IObservable<TSource> source) where TSource : IDisposable
        {
            return WithPrevious(source).Select(x =>
            {
                x.Item1?.Dispose();
                return x.Item2;
            });
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
        
        public static ObservableAsPropertyHelper<TSourceProperty> ToPropertyHelper<TSource, TSourceProperty>(
            [NotNull] this IObservable<TSourceProperty> sourceObservable,
            out ObservableAsPropertyHelper<TSourceProperty> result,
            [NotNull] TSource instance,
            [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
            [CanBeNull] IScheduler scheduler = null)
            where TSource : IDisposableReactiveObject
        {
            result = instance.ToPropertyHelper(instancePropertyExtractor, sourceObservable, default, false, scheduler);
            return result;
        }

        public static ObservableAsPropertyHelper<TSourceProperty> ToPropertyHelper<TSource, TSourceProperty>(
            [NotNull] this IObservable<TSourceProperty> sourceObservable,
            [NotNull] TSource instance,
            [NotNull] Expression<Func<TSource, TSourceProperty>> instancePropertyExtractor,
            [CanBeNull] IScheduler scheduler = null)
            where TSource : IDisposableReactiveObject
        {
            return instance.ToPropertyHelper(instancePropertyExtractor, sourceObservable, default, false, scheduler);
        }
    }
}