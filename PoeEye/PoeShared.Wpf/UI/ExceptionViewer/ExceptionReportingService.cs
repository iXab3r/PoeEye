using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualBasic.Logging;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.Wpf.Scaffolding;

namespace PoeShared.UI
{
    internal sealed class ExceptionReportingService : DisposableReactiveObject, IExceptionReportingService
    {
        private static readonly int CurrentProcessId = Process.GetCurrentProcess().Id;
        private static readonly IFluentLog Log = typeof(ExceptionReportingService).PrepareLogger();
        private readonly IAppArguments appArguments;
        private readonly IClock clock;
        private readonly IFactory<IConfigProvider> configProviderFactory;
        private readonly IFactory<IExceptionDialogDisplayer> exceptionDialogDisplayer;

        public ExceptionReportingService(
            IClock clock,
            IFolderCleanerService cleanupService,
            IFactory<IConfigProvider> configProviderFactory,
            IFactory<IExceptionDialogDisplayer> exceptionDialogDisplayer,
            IAppArguments appArguments)
        {
            this.clock = clock;
            this.configProviderFactory = configProviderFactory;
            this.exceptionDialogDisplayer = exceptionDialogDisplayer;
            this.appArguments = appArguments;
            
            Log.Debug("Initializing crashes housekeeping");
            cleanupService.AddDirectory(new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "crashes"))).AddTo(Anchors);
            cleanupService.CleanupTimeout = TimeSpan.FromHours(1);
            cleanupService.FileTimeToLive = TimeSpan.FromDays(2);
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Dispatcher.CurrentDispatcher.UnhandledException += DispatcherOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            SharedLog.Instance.Errors.SubscribeSafe(
                ex =>
                {
                    if (appArguments.IsDebugMode || Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }

                    ReportCrash(ex);
                }, Log.HandleException).AddTo(Anchors);
        }

        public Task<ExceptionDialogConfig> PrepareConfig()
        {
            return Task.Run(() => PrepareConfigSafe(null));
        }

        public async Task<IReadOnlyList<ExceptionReportItem>> PrepareReportItems(Exception exception)
        {
            return await Task.Run(() => PrepareReportItemsInternal(exception));
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
            ReportCrash(e.Exception, "TaskSchedulerUnobservedTaskException");
        }

        private void ReportCrash(Exception exception, string developerMessage = "")
        {
            Log.Error($"Unhandled application exception({developerMessage})", exception);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskSchedulerOnUnobservedTaskException;
            Dispatcher.CurrentDispatcher.UnhandledException -= DispatcherOnUnhandledException;

            var config = PrepareConfigSafe(exception);

            var reporter = exceptionDialogDisplayer.Create();
            reporter.ShowDialog(config);
            
            Log.Warn("Shutting down...");
            Environment.Exit(-1);
        }

        private ExceptionDialogConfig PrepareConfigSafe(Exception exception)
        {
            var basicConfig = new ExceptionDialogConfig
            {
                AppName = appArguments.AppName,
                Title = $"{appArguments.AppTitle} Error Report",
                Timestamp = clock.Now,
                Exception = exception
            };
            
            try
            {
                var itemsToAttach = PrepareReportItemsInternal(exception);
                return basicConfig with
                {
                   FilesToAttach = itemsToAttach.ToArray()
                };
            }
            catch (Exception e)
            {
                Log.Warn("Failed to prepare extended exception config", e);
                return basicConfig;
            }
        }

        private void TryToFormatException(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems, Exception exception)
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

        private void TryToCopyLogs(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems)
        {
            const int logsToInclude = 5;
            const int logsToAttach = 2;
            try
            {
                Log.Debug("Preparing log files for crash report...");
                var logFilesRoot = appArguments.AppDataDirectory;
                var logFilesToInclude = new DirectoryInfo(logFilesRoot)
                    .GetFiles("*.log", SearchOption.AllDirectories)
                    .OrderByDescending(x => x.LastWriteTime)
                    .Take(logsToInclude)
                    .ToArray();

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
                        reportItems.Add(new ExceptionReportItem
                        {
                            Description = $"Created: {logFile.CreationTime}\nLast Modified: {logFile.LastWriteTime}",
                            Attachment = new FileInfo(destinationFileName),
                            Attached = idx < logsToAttach
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to copy log file {logFile} to {destinationFileName}", e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed to prepare log files", e);
            }
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
                Log.Debug($"Saving configuration to {configFromMemoryPath}");
                configProviderFromFile.SaveToFile(configFromMemoryPath);
                reportItems.Add(new ExceptionReportItem()
                {
                    Description = $"In-memory config",
                    Attachment = configFromMemoryPath
                });
            }
            catch (Exception e)
            {
                Log.Warn("Failed to copy config from memory", e);
            }
        }

        private void TryToScreenshotDesktop(DirectoryInfo outputDirectory, IList<ExceptionReportItem> reportItems)
        {
            try
            {
                var screen = System.Windows.Forms.SystemInformation.VirtualScreen;
                var destinationFileName = Path.Combine(outputDirectory.FullName, $"Screen {screen.Width}x{screen.Height}.png");
                var image = UnsafeNative.GetDesktopImageViaCopyFromScreen(screen);
                image.Save(destinationFileName);
                reportItems.Add(new ExceptionReportItem()
                {
                    Description = $"Desktop screenshot",
                    Attachment = new FileInfo(destinationFileName),
                    Attached = false
                });
            }
            catch (Exception e)
            {
                Log.Warn("Failed to get desktop screenshot");
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
                    Log.Debug($"Copying existing configuration to {configCopy}");
                    File.Copy(existingConfig, configCopy.FullName);
                    reportItems.Add(new ExceptionReportItem()
                    {
                        Description = $"Copy of {existingConfig}",
                        Attachment = configCopy
                    });
                }
                catch (Exception e)
                {
                    Log.Warn($"Failed to copy existing configuration from config provider {configProvider}, config: {existingConfig}");
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed to copy existing config", e);
            }
        }

        private IReadOnlyList<ExceptionReportItem> PrepareReportItemsInternal(Exception exception)
        {
            var crashReportDirectoryPath = new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "crashes", $"{appArguments.AppName} {appArguments.Version}{(appArguments.IsDebugMode ? " DEBUG" : string.Empty)} Id{CurrentProcessId} {clock.Now.ToString($"yyyy-MM-dd HHmmss")}"));
            if (crashReportDirectoryPath.Exists)
            {
                Log.Warn($"Removing existing directory with crash data {crashReportDirectoryPath.FullName}");
                crashReportDirectoryPath.Delete(true);
            }

            Log.Debug($"Creating directory {crashReportDirectoryPath.FullName}");
            crashReportDirectoryPath.Create();

            var itemsToAttach = new List<ExceptionReportItem>();


            if (exception != null)
            {
                TryToFormatException(crashReportDirectoryPath, itemsToAttach, exception);
            }
            TryToCopyConfigFromMemory(crashReportDirectoryPath, itemsToAttach);
            TryToCopyExistingConfig(crashReportDirectoryPath, itemsToAttach);
            TryToCopyLogs(crashReportDirectoryPath, itemsToAttach);
            TryToScreenshotDesktop(crashReportDirectoryPath, itemsToAttach);
            return itemsToAttach;
        }
    }
}