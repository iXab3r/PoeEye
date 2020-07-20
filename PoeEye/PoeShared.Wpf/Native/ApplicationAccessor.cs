using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationAccessor));

        public ApplicationAccessor()
        {
            WhenExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => Application.Current.Exit += h,
                    h => Application.Current.Exit -= h)
                .Select(x => x.EventArgs)
                .ToUnit();

            WhenExit.Subscribe(() => Log.Info($"Application exit requested"), Log.HandleException).AddTo(Anchors);
        }

        public IObservable<Unit> WhenExit { get; }
    }
}