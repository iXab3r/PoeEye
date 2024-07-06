using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using DynamicData;
using Microsoft.JSInterop;
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
    private readonly IFactory<IReportItemsAggregator, ExceptionDialogConfig> reportItemsAggregatorFactory;
    private readonly IFactory<IExceptionDialogDisplayer, IReportItemsAggregator> exceptionDialogDisplayer;
    private readonly SourceListEx<IExceptionReportItemProvider> reportItemProviders = new();
    private readonly SourceListEx<IExceptionInterceptor> exceptionInterceptors = new();
    private readonly NamedLock exceptionReportGate = new NamedLock("ExceptionReport");
    private IExceptionReportHandler reportHandler;

    public ExceptionReportingService(
        IClock clock,
        IApplicationAccessor applicationAccessor,
        IFolderCleanerService cleanupService,
        IFactory<IReportItemsAggregator, ExceptionDialogConfig> reportItemsAggregatorFactory,
        IFactory<IExceptionDialogDisplayer, IReportItemsAggregator> exceptionDialogDisplayer,
        IFactory<MetricsReportProvider> metricsProviderFactory,
        IFactory<CopyLogsExceptionReportProvider> copyLogsProviderFactory,
        IFactory<AppScreenshotReportItemProvider> appScreenshotReportProviderFactory,
        IFactory<DesktopScreenshotReportItemProvider> screenshotReportProviderFactory,
        IFactory<CopyConfigReportItemProvider> configReportProviderFactory,
        IFactory<WindowsEventLogReportItemProvider> windowsEventLogReportProviderFactory,
        IAppArguments appArguments)
    {
        this.clock = clock;
        this.applicationAccessor = applicationAccessor;
        this.reportItemsAggregatorFactory = reportItemsAggregatorFactory;
        this.exceptionDialogDisplayer = exceptionDialogDisplayer;
        this.appArguments = appArguments;

        Log.Debug("Initializing crashes housekeeping");
        cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.RoamingAppDataDirectory, "reports"))).AddTo(Anchors);
        cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "reports"))).AddTo(Anchors);
        cleanupService.CleanupTimeout = TimeSpan.FromHours(12);
        cleanupService.FileTimeToLive = TimeSpan.FromDays(1);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

        Binder.SetExceptionHandler(BinderExceptionHandler);

        SharedLog.Instance.Errors.SubscribeSafe(
            ex => { ReportCrash(ex); }, Log.HandleException).AddTo(Anchors);

        AddReportItemProvider(appScreenshotReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(configReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(copyLogsProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(metricsProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(windowsEventLogReportProviderFactory.Create()).AddTo(Anchors);
        AddReportItemProvider(screenshotReportProviderFactory.Create()).AddTo(Anchors);
    }

    public void SetReportConsumer(IExceptionReportHandler reportHandler)
    {
        if (this.reportHandler != null)
        {
            throw new InvalidOperationException($"Report consumer is already configured to {this.reportHandler}");
        }

        Log.Debug($"Setting report consumer to {this.reportHandler}");
        this.reportHandler = reportHandler;
    }

    public IDisposable AddReportItemProvider(IExceptionReportItemProvider reportItemProvider)
    {
        Log.Debug($"Registering new report item provider: {reportItemProvider}");
        reportItemProviders.Add(reportItemProvider);
        return Disposable.Create(() =>
        {
            Log.Debug($"Removing report item provider: {reportItemProvider}");
            reportItemProviders.Remove(reportItemProvider);
        });
    }

    public IDisposable AddExceptionInterceptor(IExceptionInterceptor exceptionInterceptor)
    {
        Log.Debug($"Registering new exception interceptor: {exceptionInterceptor}");
        exceptionInterceptors.Add(exceptionInterceptor);
        return Disposable.Create(() =>
        {
            Log.Debug($"Removing exception interceptor: {exceptionInterceptor}");
            exceptionInterceptors.Remove(exceptionInterceptor);
        });
    }

    public void ReportProblem()
    {
        ShowExceptionDialog(default(Exception));
    }

    private void ShowExceptionDialog(Exception exception)
    {
        Log.Info($"Preparing config for exception {exception}");
        var config = PrepareConfigSafe(exception);

        Log.Info("Creating report items aggregator");
        using var itemsAggregator = reportItemsAggregatorFactory.Create(config);

        Log.Info("Giving some time for collecting mission-critical data before altering program state");
        //FIXME Dirty hack to allow appScreenshot reporter to capture screenshots of the app without Exception Dialog
        Thread.Sleep(1000);

        Log.Info("Sending signal to show exception dialog window");
        var reporter = exceptionDialogDisplayer.Create(itemsAggregator);
        reporter.ShowDialog(config);
        Log.Info("Exception dialog window was closed");
    }

    private void BinderExceptionHandler(object sender, BindingExceptionEventArgs e)
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
        if (!e.Observed)
        {
            if (e.Exception.InnerException?.GetType().Name is "RpcException" or
                "Http2ConnectionException" or
                nameof(OperationCanceledException) or
                nameof(TaskCanceledException) or
                nameof(TimeoutException) or
                nameof(IOException))
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
                Log.Warn("Suppressing known unobserved exception", e.Exception.InnerException);
                e.SetObserved();
                return;
            } else if (e.Exception.InnerException is JSException jsException)
            {
                Log.Warn("Suppressing JS exception", jsException);
                e.SetObserved();
                return;
            }
        }

        if (!e.Observed)
        {
            foreach (var exceptionInterceptor in exceptionInterceptors.Items)
            {
                exceptionInterceptor.Handle(e);
                if (e.Observed)
                {
                    break;
                }
            }
        }

        if (!e.Observed)
        {
            ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
        }
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


        ShowExceptionDialog(exception);

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