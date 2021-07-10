using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Services
{
    internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
    {
        private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger();

        public ApplicationAccessor()
        {
            var application = Application.Current;
            Log.Debug($"Binding to application {application}");
            var onExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => Application.Current.Exit += h, h => Application.Current.Exit -= h)
                .Select(x => x.EventArgs)
                .ToUnit()
                .Publish();
            WhenExit = onExit;
            WhenExit.SubscribeSafe(() => Log.Info($"Application exit requested"), Log.HandleException).AddTo(Anchors);
            onExit.Connect().AddTo(Anchors);
            
        }

        public IObservable<Unit> WhenExit { get; }
    }
}