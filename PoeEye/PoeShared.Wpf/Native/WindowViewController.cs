using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using log4net;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    public sealed class WindowViewController : DisposableReactiveObject, IWindowViewController
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowViewController));

        private readonly Window owner;
        private bool topmost;

        public WindowViewController(Window owner)
        {
            this.owner = owner;
            Log.Debug($"[{owner}.{owner.Title}] Binding ViewController to window, {new {owner.IsLoaded, owner.RenderSize, owner.Title, owner.WindowState, owner.ShowInTaskbar}}");

            WhenRendered = Observable
                .FromEventPattern<EventHandler, EventArgs>(h => owner.ContentRendered += h, h => owner.ContentRendered -= h)
                .ToUnit();
            WhenUnloaded = Observable
                .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Unloaded += h, h => owner.Unloaded -= h)
                .ToUnit();
            WhenLoaded = Observable.Merge(
                Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => owner.Loaded += h, h => owner.Loaded -= h).ToUnit(),
                Observable.Return(Unit.Default).Where(x => owner.IsLoaded).ToUnit())
                .Take(1);

            this.WhenAnyValue(x => x.Topmost)
                .Subscribe(x => owner.Topmost = x)
                .AddTo(Anchors);
        }

        public IObservable<Unit> WhenLoaded { get; }
        
        public IObservable<Unit> WhenUnloaded { get; }

        public IObservable<Unit> WhenRendered { get; }

        public bool Topmost
        {
            get => topmost;
            set => this.RaiseAndSetIfChanged(ref topmost, value);
        }

        public void Hide()
        {
            Log.Debug($"[{owner}.{owner.Title}] Hiding window");
            UnsafeNative.HideWindow(owner);
        }

        public void Show()
        {
            Log.Debug($"[{owner}.{owner.Title}] Showing window");
            UnsafeNative.ShowWindow(owner);
            //FIXME Mahapps window resets topmost after minimize/maximize operations
            owner.Topmost = topmost;
        }

        public void Minimize()
        {
            Log.Debug($"[{owner}.{owner.Title}] Minimizing window");
            owner.WindowState = WindowState.Minimized;
        }
    }
}