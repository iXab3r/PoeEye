using System;
using System.Collections.Generic;
using System.IO;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Reporting;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

internal sealed class GcLogReportProvider : DisposableReactiveObject, IErrorReportItemProvider
{
    private static readonly IFluentLog Log = typeof(GcLogReportProvider).PrepareLogger();

    private readonly IAppArguments appArguments;

    public GcLogReportProvider(IAppArguments appArguments)
    {
        this.appArguments = appArguments;
        LogFilePath = new FileInfo(Path.Combine(appArguments.AppDataDirectory, "logs", $"gc{(appArguments.IsDebugMode ? "DebugMode" : null)}.csv"));
        Log.Info("Gc log report provider has been initialized");
    }
    
    public FileInfo LogFilePath { get; }
    
    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        Log.Debug("Preparing GC log files for report...");
        LogFilePath.Refresh();

        if (!LogFilePath.Exists)
        {
            Log.Warn($"For some reason GC log file is not available @ {LogFilePath.FullName}");
            yield break;
        }

        var outputFilePath = Path.Combine(outputDirectory.FullName, "logs", LogFilePath.Name);
        Log.Debug($"Copying GC log file {LogFilePath.FullName} ({LogFilePath.Length}b) to {outputFilePath}");
        LogFilePath.CopyTo(outputFilePath, overwrite: true);
        Log.Debug($"Copied GC log file to {outputFilePath}");

        yield return new ExceptionReportItem()
        {
            Attached = true,
            Attachment = new FileInfo(outputFilePath),
            Description = ".NET Garbage Collector metrics"
        };
    }
}