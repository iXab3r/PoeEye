using System;
using System.IO;
using System.Reactive.Concurrency;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.UI;

internal sealed class ReportItemsAggregator : DisposableReactiveObject, IReportItemsAggregator
{
    private static readonly IFluentLog Log = typeof(ReportItemsAggregator).PrepareLogger();
    private static readonly int CurrentProcessId = Environment.ProcessId;

    private readonly ExceptionDialogConfig config;
    private readonly IClock clock;
    private readonly IAppArguments appArguments;

    private readonly SourceListEx<ExceptionReportItem> reportItems = new();

    public ReportItemsAggregator(
        ExceptionDialogConfig config,
        IClock clock,
        IAppArguments appArguments,
        [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
    {
        Log.Info($"Report items aggregator initialized, config: {config}");
        if (!config.IsValid)
        {
            throw new ArgumentException($"Provided exception dialog config is not valid: {config}");
        }
        this.config = config;
        this.clock = clock;
        this.appArguments = appArguments;
        bgScheduler.Schedule(() =>
        {
            Log.Debug("Retrieving report items");
            PrepareReportItemsInternal();
            Log.Debug($"Retrieved {reportItems.Count} report items");
        }).AddTo(Anchors);
    }

    public bool IsReady { get; [UsedImplicitly] private set; }
    
    public string Status { get; private set; }

    public IObservableList<ExceptionReportItem> ReportItems => reportItems;

    private void PrepareReportItemsInternal()
    {
        try
        {
            Log.Info($"Preparing report items infrastructure");
            Status = "Preparing infrastructure...";
            var crashReportDirectoryPath = new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "reports", $"{appArguments.AppName} {appArguments.Version} {appArguments.Profile} Id{CurrentProcessId} {clock.Now.ToString($"yyyy-MM-dd HHmmss")}"));
            if (crashReportDirectoryPath.Exists)
            {
                Log.Warn($"Removing existing directory with crash data {crashReportDirectoryPath.FullName}");
                crashReportDirectoryPath.Delete(true);
            }

            Log.Debug($"Creating directory {crashReportDirectoryPath.FullName}");
            crashReportDirectoryPath.Create();

            if (config.Exception != null)
            {
                TryToFormatException(crashReportDirectoryPath, reportItems, config.Exception);
            }

            var providerIdx = 0;
            foreach (var reportItemProvider in config.ItemProviders)
            {
                providerIdx++;

                try
                {
                    Log.Debug($"Getting report item from {reportItemProvider}");
                    Status = $"Preparing report {providerIdx}/{config.ItemProviders.Length}...";

                    foreach (var item in reportItemProvider.Prepare(crashReportDirectoryPath))
                    {
                        Log.Debug($"Successfully received report item from {reportItemProvider}: {item}");
                        reportItems.Add(item);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to get report item from {reportItemProvider}", e);
                }

            }

            Status = default;
        }
        finally
        {
            IsReady = true;
        }
    }
    
    private static void TryToFormatException(DirectoryInfo outputDirectory, ISourceList<ExceptionReportItem> reportItems, Exception exception)
    {
        try
        {
            Log.Debug("Preparing exception stacktrace for crash report...");

            var destinationFileName = Path.Combine(outputDirectory.FullName, $"stacktrace.txt");
            var description = $"Exception: {exception}\n\nMessage:\n\n{exception.Message}StackTrace:\n\n{exception.StackTrace}";
            File.WriteAllText(destinationFileName, description);

            reportItems.Add(new ExceptionReportItem()
            {
                Description = description,
                Attachment = new FileInfo(destinationFileName)
            });
        }
        catch (Exception e)
        {
            Log.Warn("Failed to prepare exception trace", e);
        }
    }
}