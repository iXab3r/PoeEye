using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Modularity;
using ReactiveUI;

namespace PoeShared.Services;

internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();
    private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger();
    private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(5);
    private readonly IAppArguments appArguments;
    private readonly Application application;
    private readonly FileLock loadingFileLock;
    private readonly FileLock runningFileLock;
    private readonly ISubject<int> whenTerminated = new Subject<int>();

    public ApplicationAccessor(
        Application application,
        IAppArguments appArguments)
    {
        this.application = application;
        this.appArguments = appArguments;
        Log.Info(() => $"Initializing Application accessor for {application}");
        if (application == null)
        {
            throw new ApplicationException("Application is not initialized");
        }
            
        Log.Info(() => $"Binding to application {application}");
        WhenExit = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => application.Exit += h, h => application.Exit -= h)
            .Select(x => x.EventArgs.ApplicationExitCode)
            .Replay(1)
            .AutoConnect();
        WhenExit.SubscribeSafe(x =>
        {
            Log.Info($"Application exit requested, exit code: {x}");
            IsExiting = true;
        }, Log.HandleException).AddTo(Anchors);

        runningFileLock = new FileLock(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $".running{(appArguments.IsDebugMode ? "DebugMode" : null)}"))).AddTo(Anchors);
        loadingFileLock = new FileLock(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $".loading{(appArguments.IsDebugMode ? "DebugMode" : null)}"))).AddTo(Anchors);
        LastExitWasGraceful = !runningFileLock.ExistedInitially;
        LastLoadWasSuccessful = !loadingFileLock.ExistedInitially;
        this.WhenAnyValue(x => x.IsLoaded).Where(x => x == true).SubscribeSafe(x =>
        {
            Log.Info(() => $"Application is loaded - cleaning up lock file {loadingFileLock}");
            loadingFileLock.Dispose();
        }, Log.HandleException).AddTo(Anchors);
        WhenExit.SubscribeSafe(exitCode =>
        {
            if (exitCode == 0)
            {
                Log.Info(() => $"Graceful exit - cleaning up lock file {runningFileLock}");
                runningFileLock.Dispose();
            }
            else
            {
                Log.Warn($"Erroneous exit detected, code: {exitCode} - leaving lock file intact {runningFileLock}");
            }
        }, Log.HandleException).AddTo(Anchors);
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    public bool IsLoaded { get; private set; }
        
    public bool LastExitWasGraceful { get; }
        
    public bool LastLoadWasSuccessful { get; }
        
    public void ReportIsLoaded()
    {
        if (IsLoaded)
        {
            throw new InvalidOperationException("Application is already loaded");
        }

        Log.Info("Marking application as loaded");
        IsLoaded = true;
    }

    public IObservable<int> WhenExit { get; }

    public IObservable<int> WhenTerminate => whenTerminated;

    public bool IsExiting { get; private set; }

    public void Terminate(int exitCode)
    {
        ReportTermination(exitCode);
        Log.Warn($"Closing application via Environment.Exit with code {exitCode}");
        Environment.Exit(exitCode);
    }

    public async Task Exit()
    {
        Log.Info(() => $"Attempting to gracefully shutdown application, IsExiting: {IsExiting}");
        lock (application)
        {
            if (IsExiting)
            {
                Log.Warn("Shutdown is already in progress");
                return;
            }
            IsExiting = true;
        }

        try
        {
            Shutdown();

            Log.Info($"Awaiting for application termination for {TerminationTimeout}");
            var exitCode = await WhenExit.Take(1).Timeout(TerminationTimeout);
                
            Log.Info($"Application termination signal was processed, exit code: {exitCode}");

            await Task.Delay(TerminationTimeout);
            Log.Warn($"Application should've terminated by now");
            throw new ApplicationException("Something went wrong - application failed to Shutdown gracefully");
        }
        catch (Exception e)
        {
            Log.Warn("Failed to terminate app gracefully, forcing Terminate", e);
            Terminate(-1);
        }
    }

    private void ReportTermination(int exitCode)
    {
        Log.Warn($"Raising Terminate event with code {exitCode}");
        whenTerminated.OnNext(exitCode);
        Log.Warn($"Processed Terminate event with code {exitCode}");
    }

    private void Shutdown()
    {
        if (!application.CheckAccess())
        {
            throw new InvalidOperationException($"{nameof(Shutdown)} invoked on non-main thread");
        }
            
        Log.Info(() => $"Terminating application (shutdownMode: {application.ShutdownMode}, window: {application.MainWindow})...");
        if (application.MainWindow != null && application.ShutdownMode == ShutdownMode.OnMainWindowClose)
        {
            Log.Info(() => $"Closing main window {application.MainWindow}...");
            application.MainWindow.Close();
        }
        else
        {
            Log.Info(() => $"Closing app via Shutdown");
            application.Shutdown(0);
        }
    }
}