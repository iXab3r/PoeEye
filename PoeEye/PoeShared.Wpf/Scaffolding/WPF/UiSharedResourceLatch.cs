using System;
using System.Windows.Threading;
using log4net;
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

        private static readonly ILog Log = LogManager.GetLogger(typeof(UiSharedResourceLatch));

        private readonly ISharedResourceLatch isBusyLatch;
        private readonly ISharedResourceLatch pauseLatch;

        private bool isBusy;
        private string name;
        private bool isPaused;

        public UiSharedResourceLatch(
            ISharedResourceLatch pauseLatch,
            ISharedResourceLatch isBusyLatch,
            [Dependency(WellKnownDispatchers.UI)] Dispatcher dispatcher)
        {
            this.pauseLatch = pauseLatch.AddTo(Anchors);
            this.isBusyLatch = isBusyLatch.AddTo(Anchors);
            
            isBusyLatch.WhenAnyValue(x => x.IsBusy)
                .SubscribeSafe(x =>
                {
                    dispatcher.Invoke(() => IsBusy = x, x ? DispatcherPriority.Send : DispatcherPriority.ApplicationIdle);
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