using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Threading;
using log4net;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Scaffolding.WPF
{
    internal sealed class UiSharedResourceLatch : DisposableReactiveObject, IUiSharedResourceLatch
    {
        private static readonly Binder<UiSharedResourceLatch> Binder = new();

        static UiSharedResourceLatch()
        {
            Binder.Bind(x => $"{x.Name}-IsBusy").To(x => x.isBusyLatch.Name);
            Binder.Bind(x => $"{x.Name}-Pause").To(x => x.pauseLatch.Name);
            Binder.Bind(x => x.pauseLatch.IsBusy).To(x => x.IsPaused);
        }

        private static readonly IFluentLog Log = typeof(UiSharedResourceLatch).PrepareLogger();

        private readonly ISharedResourceLatch isBusyLatch;
        private readonly ISharedResourceLatch pauseLatch;
        private readonly SerialDisposable isBusyAnchor;

        private bool isBusy;
        private string name;
        private bool isPaused;

        public UiSharedResourceLatch(
            ISharedResourceLatch pauseLatch,
            ISharedResourceLatch isBusyLatch,
            [Dependency(WellKnownDispatchers.UI)] Dispatcher dispatcher,
            [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiIdleScheduler)
        {
            this.isBusyAnchor = new SerialDisposable().AddTo(Anchors);
            this.pauseLatch = pauseLatch.AddTo(Anchors);
            this.isBusyLatch = isBusyLatch.AddTo(Anchors);
            
            isBusyLatch.WhenAnyValue(x => x.IsBusy)
                .SubscribeSafe(x =>
                {
                    // FIXME Rewrite me PLEASE ! Idea behind this pile of shit is the following: 
                    // 0) IsBusy controls visibility of BusyDecorator that is shown on a separate UI thread to allow showing animated loading indicator when main UI thread is processing something
                    // 1) If IsBusy is set to TRUE is must be propagated to UI asap => so either Invoke or simply assign
                    // 2) If IsBusy is set to FALSE it must be sent to UI thread, but only as a low-priority task, this will make UI thread process all other messages before
                    // 3) If there are multiple assignments next one should cancel previous, otherwise it will make state unreliable (e.g. IsBusy = false (sends to queue with low-priority), then IsBusy = tru(executed on the same thread) will not work correctly
                    isBusyAnchor.Disposable = null;
                    var isOnDispatcher = dispatcher.CheckAccess();
                    switch (x)
                    {
                        case true when isOnDispatcher:
                            IsBusy = true;
                            break;
                        case true:
                            dispatcher.Invoke(() => IsBusy = true);
                            break;
                        default:
                            isBusyAnchor.Disposable = uiIdleScheduler.Schedule(() => IsBusy = false);
                            break;
                    }
                }, Log.HandleUiException)
                .AddTo(Anchors);
            Binder.Attach(this).AddTo(Anchors);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set => RaiseAndSetIfChanged(ref isBusy, value);
        }

        public bool IsPaused
        {
            get => isPaused;
            private set => RaiseAndSetIfChanged(ref isPaused, value);
        }

        public string Name
        {
            get => name;
            set => RaiseAndSetIfChanged(ref name, value);
        }
       
        public IDisposable Rent()
        {
            return isBusyLatch.Rent();
        }

        public IDisposable Pause()
        {
            return pauseLatch.Rent();
        }
    }
}