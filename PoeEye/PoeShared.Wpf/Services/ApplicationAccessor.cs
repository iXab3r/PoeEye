using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using ReactiveUI;

namespace PoeShared.Services;

internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
{
    private static readonly int CurrentProcessId = Environment.ProcessId;
    private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger().WithSuffix($"App#{CurrentProcessId}");
    private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(5);
    private readonly IAppArguments appArguments;
    private readonly Application application;
    private readonly IWindowHandleProvider windowHandleProvider;
    private readonly FileLock loadingFileLock;
    private readonly FileLock runningFileLock;
    private readonly ISubject<int> whenTerminated = new Subject<int>();

    public ApplicationAccessor(
        Application application,
        IWindowHandleProvider windowHandleProvider,
        IAppArguments appArguments)
    {
        this.application = application;
        this.windowHandleProvider = windowHandleProvider;
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
            Log.Info(() => $"Application exit requested, exit code: {x}");
            IsExiting = true;
        }, Log.HandleException).AddTo(Anchors);

        runningFileLock = new FileLock(new FileInfo(Path.Combine(appArguments.AppDataDirectory, $".running"))).AddTo(Anchors);
        loadingFileLock = new FileLock(new FileInfo(Path.Combine(appArguments.AppDataDirectory, $".loading"))).AddTo(Anchors);
        Log.Info(() => $"Application load state: { new { runningFileLock, loadingFileLock } }");
        LastExitWasGraceful = !runningFileLock.ExistedInitially;
        LastLoadWasSuccessful = !loadingFileLock.ExistedInitially;
        this.WhenAnyValue(x => x.IsLoaded).Where(x => x == true).SubscribeSafe(x =>
        {
            Log.Info(() => $"Performing GC after app is loaded");
            GC.Collect();
            
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
    
    public Window MainWindow
    {
        get
        {
            if (application.Dispatcher.CheckAccess())
            {
                return application.MainWindow;
            }
            return application.Dispatcher.Invoke(() => application.MainWindow);
        }
    }

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

        using var processHandle = Kernel32.GetCurrentProcess();
        Log.Info(() => $"Closing application via Terminate with code {exitCode}");
        if (Kernel32.TerminateProcess(processHandle.DangerousGetHandle(), exitCode))
        {
            Log.Info(() => $"Awaiting for process to exit for {TerminationTimeout}, handle: {processHandle}");
            var waitResult = Kernel32.WaitForSingleObject(processHandle, (int) TerminationTimeout.TotalMilliseconds);
            Log.Info(() => $"Terminate process wait result: {waitResult}");
        }
        Log.Warn($"Failed to Terminate process", new Win32Exception());
        Log.Info(() => $"Closing application via Environment.Exit with code {exitCode}");
        Environment.Exit(exitCode);
    }

    public void Exit()
    {
        Log.Info(() => $"Attempting to gracefully shutdown application, IsExiting: {IsExiting}");
        lock (application)
        {
            if (IsExiting)
            {
                Log.Warn("Shutdown is already in progress");
            }
            else
            {
                IsExiting = true;
            }
        }

        try
        {
            Shutdown();
            Log.Info(() => $"Awaiting for termination for {TerminationTimeout}");
            Thread.Sleep(TerminationTimeout);
            Log.Warn($"Application should've terminated by now");
            throw new InvalidStateException("Something went wrong - application failed to Shutdown gracefully");
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
            
        Log.Info(() => $"Terminating application (shutdownMode: {application.ShutdownMode}, window: {new { application.MainWindow }})...");
        if (application.MainWindow != null && 
            application.ShutdownMode == ShutdownMode.OnMainWindowClose && 
            application.MainWindow.IsLoaded)
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