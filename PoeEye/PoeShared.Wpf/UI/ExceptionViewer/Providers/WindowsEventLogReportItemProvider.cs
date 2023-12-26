using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

[SupportedOSPlatform("windows")]
internal sealed class WindowsEventLogReportItemProvider : IExceptionReportItemProvider
{
    private static readonly IFluentLog Log = typeof(WindowsEventLogReportItemProvider).PrepareLogger();

    private readonly IClock clock;

    public WindowsEventLogReportItemProvider(IClock clock)
    {
        this.clock = clock;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        Log.Debug($"Preparing Windows EventLog records, output directory: {outputDirectory}");
        yield return PrepareItem("Application", outputDirectory, TimeSpan.FromDays(1));
        yield return PrepareItem("System", outputDirectory, TimeSpan.FromDays(1));
        Log.Debug("Successfully prepared Windows EventLog records");
    }

    private ExceptionReportItem PrepareItem(string logName, DirectoryInfo outputDirectory, TimeSpan? timePeriod)
    {
        var destinationFileName = Path.Combine(outputDirectory.FullName, $"EventLog_{logName}.log");
        var minDate = timePeriod == null ? default(DateTime?) : clock.Now - timePeriod.Value;

        Log.Debug($"Saving EventLog records from journal {logName} {(minDate != default ? $"no earlier than {minDate}" : default)} into {destinationFileName}");
        var eventLog = new EventLog(logName);
        var recordsSaved = 0;
        using (var writer = new StreamWriter(new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
        {
            writer.WriteLine($"MachineName: {eventLog.MachineName}");
            var entryLogMessageBuilder = new StringBuilder();
            for (var idx = eventLog.Entries.Count - 1; idx >= 0; idx--)
            {
                var entry = eventLog.Entries[idx];
                if (minDate != null && entry.TimeGenerated <= minDate)
                {
                    break;
                }

                entryLogMessageBuilder.Append($"{entry.TimeGenerated:yyyy-MM-dd HH:mm:ss.fff} [{entry.Source} Id {entry.InstanceId}] {entry.EntryType} {entry.Message}");
                if (entry.Data?.Length > 0)
                {
                    entryLogMessageBuilder.Append($"\nData:\n{entry.Data.DumpToHex()}");
                }
                writer.WriteLine(entryLogMessageBuilder);
                entryLogMessageBuilder.Clear();
                recordsSaved++;
            }
        }
        Log.Debug($"Saved {recordsSaved}/{eventLog.Entries.Count} to {destinationFileName}");

        return new ExceptionReportItem
        {
            Attached = true,
            Attachment = new FileInfo(destinationFileName),
            Description = $"Windows Event Log, journal {logName}{(minDate != null ? $"" : default)}\n{File.ReadAllText(destinationFileName).TakeChars(ushort.MaxValue)}"
        };
    }
}