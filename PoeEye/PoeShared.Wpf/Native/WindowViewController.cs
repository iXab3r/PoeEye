using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;

namespace PoeShared.Native
{
    public sealed class WindowViewController : DisposableReactiveObject, IViewController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowViewController));

        private readonly Window owner;
        private readonly ISubject<Unit> whenLoaded = new ReplaySubject<Unit>(1);

        public WindowViewController(Window owner)
        {
            this.owner = owner;
            Observable.Merge(
                    Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Loaded += h, h => owner.Loaded -= h).ToUnit(),
                    Observable.Return(Unit.Default).Where(x => owner.IsLoaded))
                .Take(1)
                .Subscribe(x => OnLoaded())
                .AddTo(Anchors);
            
            Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Unloaded += h, h => owner.Unloaded -= h).ToUnit()
                .Take(1)
                .Subscribe(x => OnUnloaded())
                .AddTo(Anchors);
        }

        public IObservable<Unit> WhenLoaded => whenLoaded;
        
        public void Hide()
        {
            Log.Debug($"[{owner}.{owner.Title}] Hiding window");
            UnsafeNative.HideWindow(owner);
        }

        public void Show()
        {
            Log.Debug($"[{owner}.{owner.Title}] Showing window");
            UnsafeNative.ShowWindow(owner);
        }

        private void OnUnloaded()
        {
            Log.Debug($"[{owner}.{owner.Title}] Window unloaded");
        }

        private void OnLoaded()
        {
            Log.Debug($"[{owner}.{owner.Title}] Window loaded");
            whenLoaded.OnNext(Unit.Default);
        }
    }
}