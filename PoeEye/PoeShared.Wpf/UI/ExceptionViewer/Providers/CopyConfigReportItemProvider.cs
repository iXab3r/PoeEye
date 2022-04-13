using System;
using System.Collections.Generic;
using System.IO;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.UI.Providers;

internal sealed class CopyConfigReportItemProvider: IExceptionReportItemProvider
{
    private static readonly IFluentLog Log = typeof(CopyConfigReportItemProvider).PrepareLogger();
    private readonly IFactory<IConfigProvider> configProviderFactory;

    public CopyConfigReportItemProvider(IFactory<IConfigProvider> configProviderFactory)
    {
        this.configProviderFactory = configProviderFactory;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        var result = new List<ExceptionReportItem>();
        TryToCopyConfigFromMemory(outputDirectory, result);
        TryToCopyExistingConfig(outputDirectory, result);
        return result;
    }

    private void TryToCopyConfigFromMemory(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems)
    {
        try
        {
            var configProvider = configProviderFactory.Create();
            if (configProvider is not ConfigProviderFromFile configProviderFromFile)
            {
                return;
            }

            Log.Debug("Preparing config dump for crash report...");
            var configFromMemoryPath = new FileInfo(Path.Combine(outputDirectory.FullName, "configDump.cfg"));
            Log.Debug(() => $"Saving configuration to {configFromMemoryPath}");
            configProviderFromFile.SaveToFile(configFromMemoryPath);
            reportItems.Add(new ExceptionReportItem()
            {
                Description = $"In-memory config",
                Attachment = configFromMemoryPath,
                Attached = false
            });
        }
        catch (Exception e)
        {
            Log.Warn("Failed to copy config from memory", e);
        }
    }

    private void TryToCopyExistingConfig(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems)
    {
        try
        {
            var configProvider = configProviderFactory.Create();
            if (configProvider is not ConfigProviderFromFile configProviderFromFile)
            {
                return;
            }

            var existingConfig = configProviderFromFile.ConfigFilePath;
            try
            {
                if (!File.Exists(existingConfig))
                {
                    return;
                }

                Log.Debug("Preparing config copy for crash report");

                var configCopy = new FileInfo(Path.Combine(outputDirectory.FullName, Path.GetFileName(existingConfig)));
                Log.Debug(() => $"Copying existing configuration to {configCopy}");
                File.Copy(existingConfig, configCopy.FullName);
                reportItems.Add(new ExceptionReportItem()
                {
                    Description = $"Copy of {existingConfig}",
                    Attachment = configCopy,
                    Attached = false
                });
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to copy existing configuration from config provider {configProvider}, config: {existingConfig}", e);
            }
        }
        catch (Exception e)
        {
            Log.Warn("Failed to copy existing config", e);
        }
    }
}