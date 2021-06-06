using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Threading;
using log4net;
using PoeShared.Prism;
using PoeShared.Services;
using ReactiveUI;
using Unity;

namespace PoeShared.Scaffolding.WPF
{
    internal sealed class UiSharedResourceLatch : DisposableReactiveObject, IUiSharedResourceLatch
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UiSharedResourceLatch));

        private readonly ISharedResourceLatch latch;

        public UiSharedResourceLatch(
            [Dependency(WellKnownDispatchers.UI)] Dispatcher dispatcher,
            ISharedResourceLatch latch)
        {
            this.latch = latch;
            latch.WhenAnyValue(x => x.IsBusy)
                .SubscribeSafe(x =>
                {
                    dispatcher.Invoke(() => IsBusy = x, x ? DispatcherPriority.Send : DispatcherPriority.ApplicationIdle);
                }, Log.HandleUiException)
                .AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.Name, latch, x => x.Name).AddTo(Anchors);
        }

        private bool isBusy;

        public bool IsBusy
        {
            get => isBusy;
            private set => RaiseAndSetIfChanged(ref isBusy, value);
        }

        public string Name
        {
            get => latch.Name;
            set => latch.Name = value;
        }

        public IDisposable Rent()
        {
            return latch.Rent();
        }
    }
}