using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Reporting;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

internal sealed class CopyLogsErrorReportProvider : IErrorReportItemProvider
{
    private static readonly IFluentLog Log = typeof(CopyLogsErrorReportProvider).PrepareLogger();
    private readonly IAppArguments appArguments;

    public CopyLogsErrorReportProvider(IAppArguments appArguments)
    {
        this.appArguments = appArguments;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        const int logsToInclude = 7;
        const int logsToAttach = 5;
        
        Log.Debug("Preparing log files for report...");

        try
        {
            FlushAppenders();
        }
        catch (Exception e)
        {
            Log.Warn("Failed to flush appenders", e);
        }
        
        var logFilesRoot = Path.Combine(appArguments.AppDataDirectory, "logs");
        var logFilesToInclude = new DirectoryInfo(logFilesRoot)
            .GetFiles("*.log", SearchOption.AllDirectories)
            .OrderByDescending(x => x.LastWriteTime)
            .Take(logsToInclude)
            .ToArray();
        Log.Debug($"{logFilesRoot} contains the following files:\n\t{logFilesToInclude.Select(y => new { y.Name, y.LastWriteTime }).DumpToTable()}");

        var result = new List<ExceptionReportItem>();
        for (var idx = 0; idx < logFilesToInclude.Length; idx++)
        {
            var logFile = logFilesToInclude[idx];
            var logFileName = logFile.FullName.Substring(logFilesRoot.Length).TrimStart('\\', '/');
            var destinationFileName = Path.Combine(outputDirectory.FullName, logFileName);
            try
            {
                Log.Debug($"Copying {logFile.FullName} ({logFile.Length}b) to {destinationFileName}");

                var destinationDirectory = Path.GetDirectoryName(destinationFileName);
                if (destinationDirectory == null)
                {
                    Log.Warn($"Failed to get directory path from destination file name {destinationFileName}");
                    continue;
                }

                Directory.CreateDirectory(destinationDirectory);
                logFile.CopyTo(destinationFileName, true);
                new ExceptionReportItem
                {
                    Description = $"Created: {logFile.CreationTime}\nLast Modified: {logFile.LastWriteTime}",
                    Attachment = new FileInfo(destinationFileName),
                    Attached = idx < logsToAttach
                }.AddTo(result);
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to copy log file {logFile} to {destinationFileName}", e);
            }
        }

        foreach (var exceptionReportItem in result)
        {
            yield return exceptionReportItem;
        }
    }

    private static void FlushAppenders()
    {
        Log.Debug("Flushing all appenders");

        var repository = (Hierarchy)  LogManager.GetRepository(Assembly.GetEntryAssembly());
        
        foreach (var appender in repository.GetAppenders().OfType<FileAppender>())
        {
            if (appender.ImmediateFlush)
            {
                continue;
            }

            try
            {
                if (appender.Flush(0))
                {
                    Log.Debug($"Flushed appender {new {appender.Name, appender.File}}");
                }
                else
                {
                    Log.Debug($"Failed to flush appender {new {appender.Name, appender.File}}");
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to flush appender {appender}", e);
            }
        }
    }
}