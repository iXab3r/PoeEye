using System;
using System.Reactive;
using System.Reactive.Linq;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action onNext)
        {
            return observable.Subscribe(_ => onNext());
        }

        public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action onNext, [NotNull] Action<Exception> onError)
        {
            return observable.Subscribe(_ => onNext(), onError);
        }

        public static IObservable<Unit> ToUnit<T>(this IObservable<T> observable)
        {
            return observable.Select(_ => Unit.Default);
        }

        public static IObservable<TResult> WithPrevious<TSource, TResult>(
            this IObservable<TSource> source,
            Func<TSource, TSource, TResult> resultSelector)
        {
            return WithPrevious(source)
                .Select(t => resultSelector(t.Item1, t.Item2));
        }

        public static IObservable<Tuple<TSource, TSource>> WithPrevious<TSource>(
            this IObservable<TSource> source)
        {
            return source.Scan(
                Tuple.Create(default(TSource), default(TSource)),
                (previous, current) => Tuple.Create(previous.Item2, current));
        }

        public static IObservable<T> RetryWithDelay<T>(this IObservable<T> source, TimeSpan timeSpan)
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

            return source.Catch(Observable.Timer(timeSpan).SelectMany(_ => source).Retry());
        }
    }
}