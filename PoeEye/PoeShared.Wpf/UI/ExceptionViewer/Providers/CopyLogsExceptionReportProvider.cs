using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

internal sealed class CopyLogsExceptionReportProvider : IExceptionReportItemProvider
{
    private static readonly IFluentLog Log = typeof(CopyLogsExceptionReportProvider).PrepareLogger();
    private readonly IAppArguments appArguments;

    public CopyLogsExceptionReportProvider(IAppArguments appArguments)
    {
        this.appArguments = appArguments;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        const int logsToInclude = 7;
        const int logsToAttach = 5;
        Log.Debug("Preparing log files for report...");
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
}