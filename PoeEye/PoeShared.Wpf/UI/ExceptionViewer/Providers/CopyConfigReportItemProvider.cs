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
    private readonly IAppArguments appArguments;
    private readonly IFactory<IConfigProvider> configProviderFactory;

    public CopyConfigReportItemProvider(
        IAppArguments appArguments,
        IFactory<IConfigProvider> configProviderFactory)
    {
        this.appArguments = appArguments;
        this.configProviderFactory = configProviderFactory;
    }

    public IEnumerable<ExceptionReportItem> Prepare(DirectoryInfo outputDirectory)
    {
        var result = new List<ExceptionReportItem>();
        TryToCopyConfigFromMemory(outputDirectory, result);

        var configProvider = configProviderFactory.Create();
        TryToCopyExistingConfig(outputDirectory, result);
        return result;
    }

    private void TryToCopyConfigFromMemory(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems)
    {
        try
        {
            var configProvider = configProviderFactory.Create();
            if (configProvider is not IConfigProviderFromFile configProviderFromFile)
            {
                return;
            }

            Log.Debug("Preparing config dump for crash report...");
            var configFromMemoryPath = new FileInfo(Path.Combine(outputDirectory.FullName, "configDump.cfg"));
            Log.Debug($"Saving configuration to {configFromMemoryPath}");
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

    private void TryToCopyExistingConfig(
        DirectoryInfo outputDirectory,
        IList<ExceptionReportItem> reportItems)
    {
        try
        {
            var configProvider = configProviderFactory.Create();
            if (configProvider is IConfigProviderFromFile configProviderFromFile)
            {
                TryToCopyExistingConfig(configProviderFromFile, outputDirectory, reportItems);
            } 
            else
            {
                Log.Warn($"Config provider of type {configProvider} is not supported");
            }
        }
        catch (Exception e)
        {
            Log.Warn("Failed to copy existing config", e);
        }
    }

    private static void TryToCopyExistingConfig(
        IConfigProviderFromFile configProvider,
        DirectoryInfo outputDirectory, 
        IList<ExceptionReportItem> reportItems)
    {
        var existingConfig = configProvider.ConfigFilePath;
        try
        {
            if (!File.Exists(existingConfig))
            {
                return;
            }

            Log.Debug("Preparing config copy for crash report");

            var configCopy = new FileInfo(Path.Combine(outputDirectory.FullName, Path.GetFileName(existingConfig)));
            Log.Debug($"Copying existing configuration to {configCopy}");
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
}