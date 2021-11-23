using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers
{
    internal sealed class MetricsReportProvider : IExceptionReportItemProvider
    {
        private static readonly IFluentLog Log = typeof(MetricsReportProvider).PrepareLogger();

        private readonly IAppArguments appArguments;

        public MetricsReportProvider(IAppArguments appArguments)
        {
            this.appArguments = appArguments;
        }

        public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
        {
            Log.Debug("Preparing log files for crash report...");
            var logFilesRoot = Path.Combine(appArguments.AppDataDirectory, "logs");
            var filesToInclude = new DirectoryInfo(logFilesRoot)
                .GetFiles("metrics*.txt", SearchOption.TopDirectoryOnly)
                .OrderByDescending(x => x.LastWriteTime)
                .ToArray();

            var result = new List<ExceptionReportItem>();
            foreach (var logFile in filesToInclude)
            {
                var destinationFileName = Path.Combine(outputDirectory.FullName, logFile.Name);
                try
                {
                    Log.Debug($"Copying {logFile.FullName} ({logFile.Length}b) to {destinationFileName}");
                    logFile.CopyTo(destinationFileName, true);
                    new ExceptionReportItem
                    {
                        Description = $"Performance metrics",
                        Attachment = new FileInfo(destinationFileName),
                        Attached = true
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
}