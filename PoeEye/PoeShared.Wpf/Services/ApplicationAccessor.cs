﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Services
{
    internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
    {
        private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger();
        private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(5);
        private readonly Application application;

        private readonly IObservable<ExitEventArgs> whenExit;
        private bool isExiting;

        public ApplicationAccessor()
        {
            Log.Debug("Initializing Application accessor");
            application = Application.Current;
            if (application == null)
            {
                throw new ApplicationException("Application is not initialized");
            }
            
            Log.Debug($"Binding to application {application}");
            whenExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => application.Exit += h, h => application.Exit -= h)
                .Select(x => x.EventArgs)
                .Replay(1);
            whenExit.SubscribeSafe(x =>
            {
                Log.Info($"Application exit requested, exit code: {x.ApplicationExitCode}");
                IsExiting = true;
            }, Log.HandleException).AddTo(Anchors);
        }

        public IObservable<Unit> WhenExit => whenExit.ToUnit();

        public bool IsExiting
        {
            get => isExiting;
            private set => RaiseAndSetIfChanged(ref isExiting, value);
        }

        public async Task Exit()
        {
            Log.Debug($"Attempting to gracefully shutdown application, IsExiting: {isExiting}");
            lock (application)
            {
                if (isExiting)
                {
                    Log.Warn("Shutdown is already in progress");
                    return;
                }
                IsExiting = true;
            }
            Shutdown();

            try
            {
                Log.Info($"Awaiting for application termination for {TerminationTimeout}");
                var closeEvent = await whenExit.Take(1).Timeout(TerminationTimeout);
                
                Log.Info($"Application termination signal was processed, exit code: {closeEvent.ApplicationExitCode}");

                await Task.Delay(TerminationTimeout);
                Log.Warn($"Application should've terminated by now");
                throw new ApplicationException("Something went wrong - application failed to Shutdown gracefully");
            }
            catch (Exception e)
            {
                Log.Warn("Failed to terminate app gracefully, forcing Environment.Exit", e);
                Environment.Exit(0);
            }
        }

        private void Shutdown()
        {
            if (!application.CheckAccess())
            {
                Log.Debug("Rescheduling to Application dispatcher");
                application.Dispatcher.BeginInvoke(Shutdown, DispatcherPriority.Send);
                return;
            }
            
            Log.Debug($"Terminating application (shutdownMode: {application.ShutdownMode}, window: {application.MainWindow})...");
            if (application.MainWindow != null && application.ShutdownMode == ShutdownMode.OnMainWindowClose)
            {
                Log.Debug($"Closing main window {application.MainWindow}...");
                application.MainWindow.Close();
            }
            else
            {
                Log.Debug($"Closing app via Shutdown");
                application.Shutdown(0);
            }
        }
    }
}