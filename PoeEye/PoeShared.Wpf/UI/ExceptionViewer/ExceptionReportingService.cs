using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Threading;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI.Providers;
using PropertyBinder;

namespace PoeShared.UI;

internal sealed class ExceptionReportingService : DisposableReactiveObject, IExceptionReportingService
{
    private static readonly IFluentLog Log = typeof(ExceptionReportingService).PrepareLogger();
    private readonly IAppArguments appArguments;
    private readonly IClock clock;
    private readonly IApplicationAccessor applicationAccessor;
    private readonly IFactory<IExceptionDialogDisplayer> exceptionDialogDisplayer;
    private readonly SourceList<IExceptionReportItemProvider> reportItemProviders = new();
    private readonly NamedLock exceptionReportGate = new NamedLock("ExceptionReport");
    private IExceptionReportHandler reportHandler;

    public ExceptionReportingService(
        IClock clock,
        IApplicationAccessor applicationAccessor,
        IFolderCleanerService cleanupService,
        IFactory<IExceptionDialogDisplayer> exceptionDialogDisplayer,
        IFactory<MetricsReportProvider> metricsProviderFactory,
        IFactory<CopyLogsExceptionReportProvider> copyLogsProviderFactory,
        IFactory<DesktopScreenshotReportItemProvider> screenshotReportProviderFactory,
        IFactory<CopyConfigReportItemProvider> configReportProviderFactory,
        IFactory<ReportLastLogEventsProvider> lastLogEventsReportProviderFactory,
        IFactory<GcLogReportProvider> gcLogReportProviderFactory,
        IFactory<WindowsEventLogReportItemProvider> windowsEventLogReportProviderFactory,
        IAppArguments appArguments)
    {
        this.clock = clock;
        this.applicationAccessor = applicationAccessor;
        this.exceptionDialogDisplayer = exceptionDialogDisplayer;
        this.appArguments = appArguments;

        Log.Debug("Initializing crashes housekeeping");
        cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "crashes"))).AddTo(Anchors);
        cleanupService.CleanupTimeout = TimeSpan.FromHours(1);
        cleanupService.FileTimeToLive = TimeSpan.FromDays(2);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

        Binder.SetExceptionHandler(BinderExceptionHandler);

        SharedLog.Instance.Errors.SubscribeSafe(
            ex => { ReportCrash(ex); }, Log.HandleException).AddTo(Anchors);

        AddReportItemProvider(lastLogEventsReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(gcLogReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(metricsProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(configReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(copyLogsProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(windowsEventLogReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(screenshotReportProviderFactory.Create()).AddTo(Anchors);
    }

    public Task<ExceptionDialogConfig> PrepareConfig()
    {
        return Task.Run(() => PrepareConfigSafe(null));
    }

    public void SetReportConsumer(IExceptionReportHandler reportHandler)
    {
        if (this.reportHandler != null)
        {
            throw new InvalidOperationException($"Report consumer is already configured to {this.reportHandler}");
        }
        Log.Debug(() => $"Setting report consumer to {this.reportHandler}");
        this.reportHandler = reportHandler;
    }

    public IDisposable AddReportItemProvider(IExceptionReportItemProvider reportItemProvider)
    {
        Log.Debug(() => $"Registering new report item provider: {reportItemProvider}");
        reportItemProviders.Add(reportItemProvider);
        return Disposable.Create(() =>
        {
            Log.Debug(() => $"Removing report item provider: {reportItemProvider}");
            reportItemProviders.Remove(reportItemProvider);
        });
    }

    private void BinderExceptionHandler(object sender, ExceptionEventArgs e)
    {
        ReportCrash(e.Exception, $"BinderException, sender: {sender}");
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ReportCrash(e.ExceptionObject as Exception, "CurrentDomainUnhandledException");
    }

    private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportCrash(e.Exception, "DispatcherUnhandledException");
    }

    private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        if (!e.Observed && e.Exception.InnerExceptions.Count == 1 && e.Exception.InnerExceptions[0].GetType().Name is "RpcException" or "Http2ConnectionException")
        {
            //FIXME There should be a smarter way of solving that problem with unobserved GRPC RpcException/Http exceptions
            /*
             * There is a problem with Grpc.Net.Client.Internal.GrpcCall, specifically with how its method RunCall is called:
             * private async Task RunCall(HttpRequestMessage request, TimeSpan? timeout)
             * this is how current code uses it in multiple places: 
             * _ = RunCall(message, timeout);
             * This means that IF something happens inside RunCall the entire app will blow up as this exception will be gathered when Task is finalized
             * most usual errors in RunCall are HTTP and Rpc exceptions
             */
            Log.Warn("Suppressing unobserved GRPC RPC connection exception", e.Exception.InnerException);
            e.SetObserved();
            return;
        }
        ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
    }

    private void ReportCrash(Exception exception, string developerMessage = "")
    {
        Log.Error($"Unhandled application exception({developerMessage})", exception);
        using var @lock = exceptionReportGate.Enter();
        if (appArguments.IsDebugMode || Debugger.IsAttached)
        {
            Debugger.Break();
        }

        AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
        Dispatcher.CurrentDispatcher.UnhandledException -= DispatcherOnUnhandledException;

        var config = PrepareConfigSafe(exception);

        var reporter = exceptionDialogDisplayer.Create();
        reporter.ShowDialog(config);

        Log.Warn("Shutting down...");
        applicationAccessor.Terminate(-1);
    }

    private ExceptionDialogConfig PrepareConfigSafe(Exception exception)
    {
        var basicConfig = new ExceptionDialogConfig
        {
            AppName = appArguments.AppName,
            Title = $"{appArguments.AppTitle} Error Report",
            Timestamp = clock.Now,
            ReportHandler = reportHandler,
            Exception = exception
        };

        try
        {
            return basicConfig with
            {
                ItemProviders = reportItemProviders.Items.ToArray()
            };
        }
        catch (Exception e)
        {
            Log.Warn("Failed to prepare extended exception config", e);
            return basicConfig;
        }
    }
}