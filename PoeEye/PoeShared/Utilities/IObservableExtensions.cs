namespace PoeShared.Utilities
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    using JetBrains.Annotations;

    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, [NotNull] Action action)
        {
            return observable.Subscribe(_ => action());
        }

        public static IObservable<Unit> ToUnit<T>(this IObservable<T> observable)
        {
            return observable.Select(_ => Unit.Default);
        }
    }
}