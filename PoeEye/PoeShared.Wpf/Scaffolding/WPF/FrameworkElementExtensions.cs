using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

namespace PoeShared.Scaffolding.WPF
{
    public static class FrameworkElementExtensions
    {
        public static IObservable<Unit> WhenLoaded(this FrameworkElement window)
        {
            if (window == default)
            {
                return Observable.Never<Unit>();
            }

            if (!window.CheckAccess())
            {
                return window.Dispatcher.Invoke(() => WhenLoaded(window));
            }

            if (window.IsLoaded)
            {
                return Observable.Return(Unit.Default);
            }

            return Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => window.Loaded += h, h => window.Loaded -= h)
                .Take(1)
                .Select(_ => Unit.Default);
        }
    }
}