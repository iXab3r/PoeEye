using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32.TaskScheduler;
using PInvoke;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using ReactiveUI;
using Unity;

namespace PoeShared.Services;

internal sealed class ApplicationAccessor : DisposableReactiveObject, IApplicationAccessor
{
    private static readonly int CurrentProcessId = Environment.ProcessId;
    private static readonly IFluentLog Log = typeof(ApplicationAccessor).PrepareLogger().WithSuffix($"PID#{CurrentProcessId}");
    private static readonly TimeSpan TerminationTimeout = TimeSpan.FromSeconds(5);
    private readonly IAppArguments appArguments;
    private readonly Application application;
    private readonly IClock clock;
    private readonly IWindowHandleProvider windowHandleProvider;
    private readonly IUniqueIdGenerator idGenerator;
    private readonly ISafeModeService safeModeService;
    private readonly FileMarker loadingFileMarker;
    private readonly FileMarker runningFileMarker;
    private readonly ISubject<int> whenTerminated = new Subject<int>();
    private readonly DateTimeOffset startupTimestamp;
    private DateTimeOffset loadedTimestamp;

    public ApplicationAccessor(
        Application application,
        IClock clock,
        IWindowHandleProvider windowHandleProvider,
        IUniqueIdGenerator idGenerator,
        ISafeModeService safeModeService,
        IAppArguments appArguments)
    {
        Log.AddSuffix($"v{appArguments.Version}");
        this.application = application;
        this.clock = clock;
        this.windowHandleProvider = windowHandleProvider;
        this.idGenerator = idGenerator;
        this.safeModeService = safeModeService;
        this.appArguments = appArguments;
        startupTimestamp = clock.Now;
        Log.Info($"Initializing Application accessor for {application}");
        if (application == null)
        {
            throw new ApplicationException("Application is not initialized");
        }

        Log.Info($"Binding to application {application}");

        var whenAppHasExited = Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(h => application.Exit += h, h => application.Exit -= h)
            .Select(x => x.EventArgs.ApplicationExitCode)
            .Replay(1)
            .AutoConnect();

        WhenExit = Observable.Amb(whenTerminated, whenAppHasExited).Take(1);
        WhenExit.SubscribeSafe(x =>
        {
            Log.Info($"Application exit requested, exit code: {x}");
            IsExiting = true;
        }, Log.HandleException).AddTo(Anchors);

        var lockFileAcquireTimeout = TimeSpan.FromSeconds(10);
        runningFileMarker = new FileMarker(new FileInfo(Path.Combine(appArguments.AppDataDirectory, $".running")), lockFileAcquireTimeout).AddTo(Anchors);
        loadingFileMarker = new FileMarker(new FileInfo(Path.Combine(appArguments.AppDataDirectory, $".loading")), lockFileAcquireTimeout).AddTo(Anchors);
        Log.Info($"Application load state: {new {runningFileLock = runningFileMarker, loadingFileLock = loadingFileMarker}}");
        LastExitWasGraceful = !runningFileMarker.ExistedInitially;
        LastLoadWasSuccessful = !loadingFileMarker.ExistedInitially;

        this.WhenAnyValue(x => x.IsLoaded).Where(x => x == true).SubscribeSafe(x =>
        {
            Log.Info($"Performing GC after app is loaded");
            GC.Collect();

            Log.Info($"Application is loaded - cleaning up lock file {loadingFileMarker}");
            loadingFileMarker.Dispose();
        }, Log.HandleException).AddTo(Anchors);
        WhenExit
            .SubscribeSafe(exitCode =>
            {
                if (exitCode == 0)
                {
                    Log.Info($"Graceful exit - cleaning up lock file {runningFileMarker}");
                    runningFileMarker.Dispose();
                    Log.Info($"Graceful exit - cleaning up lock file {loadingFileMarker}");
                    loadingFileMarker.Dispose(); // this may happen if we're restarting before the app is loaded
                }
                else
                {
                    Log.Warn($"Erroneous exit detected, code: {exitCode} - leaving lock file intact {runningFileMarker}");
                }
            }, Log.HandleException).AddTo(Anchors);

        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
    }

    public bool IsElevated => appArguments.IsElevated;

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

        loadedTimestamp = clock.Now;
        var loadTime = loadedTimestamp - startupTimestamp;
        Log.Info($"Marking application as loaded: {new {startupTimestamp, loadedTimestamp, loadTime}}");
        IsLoaded = true;
    }

    public IObservable<int> WhenExit { get; }

    public IObservable<int> WhenTerminate => whenTerminated;

    public IObservable<Unit> WhenLoaded => this.WhenAnyValue(x => x.IsLoaded).Where(x => x).Take(1).ToUnit();

    public bool IsExiting { get; private set; }

    public void Terminate(int exitCode)
    {
        ReportTermination(exitCode);

        using var processHandle = Kernel32.GetCurrentProcess();
        Log.Info($"Closing application via Terminate with code {exitCode}");
        if (Kernel32.TerminateProcess(processHandle.DangerousGetHandle(), exitCode))
        {
            Log.Info($"Awaiting for process to exit for {TerminationTimeout}, handle: {processHandle}");
            var waitResult = Kernel32.WaitForSingleObject(processHandle, (int) TerminationTimeout.TotalMilliseconds);
            Log.Info($"Terminate process wait result: {waitResult}");
        }

        Log.Warn($"Failed to Terminate process", new Win32Exception());
        Log.Info($"Closing application via Environment.Exit with code {exitCode}");
        Environment.Exit(exitCode);
    }

    public void Exit()
    {
        Log.Info($"Attempting to gracefully shutdown application, IsExiting: {IsExiting}");
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
            Log.Info($"Awaiting for termination for {TerminationTimeout}");
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

        Log.Info($"Terminating application (shutdownMode: {application.ShutdownMode}, window: {new {application.MainWindow}})...");
        var mainWindow = application.MainWindow;
        if (mainWindow != null &&
            application.ShutdownMode == ShutdownMode.OnMainWindowClose &&
            mainWindow.IsLoaded)
        {
            Log.Info($"Closing main window {mainWindow}...");
            mainWindow.Close();
            if (mainWindow is IDisposable disposable)
            {
                Log.Info($"Disposing main window {disposable}...");
                disposable.Dispose();
                Log.Info($"Disposed main window");
            }

            Log.Info($"Closed main window");
        }

        Log.Info($"Terminating app environment");
        Terminate(0); // using this instead of App.Shutdown() to avoid async issues
    }

    public void RestartAs(string processPath, string arguments = default, string verb = default)
    {
        Log.Info($"Restarting current process as '{processPath}', args: {arguments}");

        if (arguments != null && arguments.Contains('-'))
        {
            const string safeModeArgument = "--safeMode";
            if (!arguments.Contains(safeModeArgument) && safeModeService.IsInSafeMode)
            {
                arguments += $" {safeModeArgument} true"; //enforce safe-mode
            }

            Log.Info($"Supplied arguments contain '-', compressing args: {arguments}");
            RestartAs(processPath: processPath, arguments: StringUtils.ToHexGzip(arguments), verb: verb);
            return;
        }

        ReportTermination(0); // report termination to release locks, resources, etc

        var waitCmd = "Wait-Process -Id {Environment.ProcessId}";
        var newProcessArgs = string.IsNullOrEmpty(arguments) ? "" : $" -ArgumentList \"{arguments}\"";
        var startProcessCmd = $"Start-Process -FilePath '{processPath}'{newProcessArgs}";
        var startInfo = new ProcessStartInfo()
        {
            UseShellExecute = true,
            Arguments = new[] {waitCmd, startProcessCmd}.JoinStrings("; "),
            FileName = "powershell.exe",
            WindowStyle = ProcessWindowStyle.Hidden,
            Verb = verb ?? string.Empty
        };
        var startInfoString = new {startInfo.FileName, startInfo.Arguments, startInfo.Verb, startInfo.UseShellExecute};
        var newProcess = Process.Start(startInfo) ?? throw new InvalidStateException($"Failed to start new process using args: {startInfoString}");
        Log.Info($"Spawned new process: {newProcess.Id}, args: {startInfoString}");
        Log.Info($"New process state: {new {newProcess.Id, newProcess.HasExited}}");

        Exit();
        throw new InvalidOperationException("Should never hit this line");
    }

    public void RestartAsAdmin()
    {
        Log.Info("Restarting current process with admin privileges");
        RestartAs(Environment.ProcessPath, $"{appArguments.StartupArgs} --adminMode true", verb: "runas");
    }
}