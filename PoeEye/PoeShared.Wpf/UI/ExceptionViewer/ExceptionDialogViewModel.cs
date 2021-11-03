using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.Win32;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using SevenZip;
using Syroot.Windows.IO;

namespace PoeShared.UI
{
    internal sealed class ExceptionDialogViewModel : DisposableReactiveObject
    {
        private static readonly int CurrentProcessId = Environment.ProcessId;
        private static readonly IFluentLog Log = typeof(ExceptionDialogViewModel).PrepareLogger();
        private static readonly Binder<ExceptionDialogViewModel> Binder = new();

        private static readonly string ExplorerExecutablePath =
            Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");

        private readonly IAppArguments appArguments;
        private readonly IUniqueIdGenerator idGenerator;

        private readonly IClock clock;
        private readonly IClipboardManager clipboardManager;
        private readonly ICloseController closeController;
        private readonly IExceptionReportingService reportingService;
        private readonly SourceList<ExceptionReportItem> reportItems = new();

        static ExceptionDialogViewModel()
        {
            Binder.Bind(x => x.SaveReportCommand.IsBusy || x.SendReportCommand.IsBusy || !x.AllProvidersProcessed).To(x => x.IsBusy);
            Binder.BindIf(x => x.SelectedItem == null && x.Attachments.Count > 0, x => x.Attachments[0]).To(x => x.SelectedItem);
            Binder.BindIf(x => x.Config != null, x => x.Config.Title).Else(x => default).To(x => x.Title);
            Binder.BindIf(x => x.Config != null, x => x.Config.AppName).Else(x => default).To(x => x.AppName);
        }

        public ExceptionDialogViewModel(
            IClock clock,
            IClipboardManager clipboardManager,
            IAppArguments appArguments, 
            IUniqueIdGenerator idGenerator,
            IExceptionReportingService reportingService,
            ICloseController closeController)
        {
            this.clock = clock;
            this.clipboardManager = clipboardManager;
            this.appArguments = appArguments;
            this.idGenerator = idGenerator;
            this.reportingService = reportingService;
            this.closeController = closeController;

            this.RaiseWhenSourceValue(x => x.AppName, this, x => x.Config).AddTo(Anchors);
            CloseCommand = CommandWrapper.Create(closeController.Close);
            SaveReportCommand = CommandWrapper.Create(SaveReportCommandExecuted, this.WhenAnyValue(x => x.IsBusy).ObserveOnDispatcher().Select(x => x == false));
            SendReportCommand = CommandWrapper.Create(SendReportCommandExecuted, 
                Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Config).Select(x => x?.ReportHandler != null),
                    this.WhenAnyValue(x => x.IsBusy).Select(x => x == false),(hasHandler, notBusy) => hasHandler && notBusy).ObserveOnDispatcher());
            CopyStatusToClipboard = CommandWrapper.Create(() => clipboardManager.SetText(Status), 
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.IsBusy).Select(x => x == false),
                        this.WhenAnyValue(x => x.Status).Select(x => !string.IsNullOrEmpty(x)), (notBusy, hasStatus) => notBusy && hasStatus)
                .ObserveOnDispatcher());

            reportItems
                .Connect()
                .Transform(x => new ExceptionDialogSelectableItem(x))
                .ObserveOnDispatcher()
                .Bind(out var attachments)
                .SubscribeToErrors(Log.HandleException)
                .AddTo(Anchors);
            Attachments = attachments;

            this.WhenAnyValue(x => x.Config)
                .SubscribeSafe(async x =>
                {
                    Log.Debug("Config has been updated, retrieving report items");
                    await Task.Run(PrepareReportItemsInternal);
                    Log.Debug($"Retrieved {reportItems.Count} report items");
                }, Log.HandleException)
                .AddTo(Anchors);
            
            Binder.Attach(this).AddTo(Anchors);
        }

        public ExceptionDialogConfig Config { get; set; }

        public ReadOnlyObservableCollection<ExceptionDialogSelectableItem> Attachments { get; }

        public string Title { get; [UsedImplicitly] private set; }

        public string AppName { get; [UsedImplicitly] private set; }

        public string Comment { get; set; }

        public ExceptionDialogSelectableItem SelectedItem { get; set; }

        public ICommand CloseCommand { get; }

        public CommandWrapper SaveReportCommand { get; }

        public CommandWrapper SendReportCommand { get; }
        
        public CommandWrapper CopyStatusToClipboard { get; }

        public string LastSavedFile { get; set; }

        public string Status { get; private set; }

        public bool AllProvidersProcessed { get; private set; }

        public bool IsBusy { get; [UsedImplicitly] private set; }

        private async Task SendReportCommandExecuted()
        {
            Log.Debug($"Handling report via {Config.ReportHandler}");

            var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), "EyeAurasReports", GetDefaultReportName()));

            try
            {
                Log.Debug($"Saving report to temporary file {tempFile}");
                Status = "Compressing report...";
                await CompressReport(tempFile);
                Log.Debug($"Report saved to temporary file {tempFile}, sending to {Config.ReportHandler}");
                Status = "Sending report...";
                var result = await Config.ReportHandler.Handle(tempFile);
                Log.Debug($"Report sent to {Config.ReportHandler}");
                Status = $"Report Id: {result}";
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send report to {Config.ReportHandler}", ex);
                Status = "Failed to send report";
                throw;
            }
            finally
            {
                tempFile.Refresh();
                if (tempFile.Exists)
                {
                    Log.Debug("Cleaning up temporary file");
                    tempFile.Delete();
                }
            }
        }

        private string GetDefaultReportName()
        {
            return $"{appArguments.AppName}_{appArguments.Version}{(appArguments.IsDebugMode ? "_DEBUG" : string.Empty)}_{idGenerator.Next()}_{clock.Now.ToString($"yyyy-MM-dd_HHmmss")}.7z";
        }

        private async Task SaveReportCommandExecuted()
        {
            Status = "Selecting directory...";
            var initialDirectory = Path.GetDirectoryName(LastSavedFile);
            var defaultFileName = GetDefaultReportName();

            var op = new SaveFileDialog
            {
                Title = "Select where to save report",
                FileName = defaultFileName,
                InitialDirectory = !string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory)
                    ? initialDirectory
                    : KnownFolders.Downloads.ExpandedPath,
                Filter = "7z Archive|*.7z;|All files|*.*"
            };
            if (op.ShowDialog() != true)
            {
                Log.Info("User cancelled SaveFileDialog");
                Status = default;
                return;
            }
            LastSavedFile = op.FileName;

            Status = "Compressing report...";
            await CompressReport(new FileInfo(op.FileName));
            Log.Debug("Compression completed, opening Explorer");

            Status = "Compressing report...";
            Log.Debug($"Opening link: {op.FileName}");
            var process = Process.Start(ExplorerExecutablePath, $"/select,\"{op.FileName}\"");
            if (process == null)
            {
                throw new ApplicationException("Failed to open Explorer");
            }

            await process.WaitForExitAsync();
            Status = "Report saved";
        }

        private async Task CompressReport(FileInfo outputFile)
        {
            Log.Debug($"Compressing report to {outputFile}");
            var filesToAttach = new List<string>();
            Attachments.Where(x => x.IsChecked).Select(x => x.Item.Attachment.FullName).ForEach(filesToAttach.Add);

            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            using var removeOnClose = Disposable.Create(() => Directory.Delete(tempDirectory, true));

            if (!string.IsNullOrEmpty(Comment))
            {
                var fileWithComment = Path.Combine(tempDirectory, "comment.txt");
                await File.WriteAllTextAsync(fileWithComment, Comment);
                filesToAttach.Add(fileWithComment);
            }

            var outputDirectory = outputFile.Directory;
            if (outputDirectory == null)
            {
                throw new InvalidOperationException($"Something went wrong - directory of output {outputFile} is null");
            }

            if (!outputDirectory.Exists)
            {
                Log.Debug($"Creating output directory {outputDirectory}");
                outputDirectory.Create();
            }

            await Task.Run(() =>
            {
                var newArchive = new SevenZipCompressor();
                Log.Debug($"Compressing report with following files: {filesToAttach.DumpToTable()}");
                newArchive.DirectoryStructure = false;
                newArchive.CompressionLevel = CompressionLevel.Normal;
                newArchive.CompressFiles(outputFile.FullName, filesToAttach.ToArray());
            });
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

        private void PrepareReportItemsInternal()
        {
            reportItems.Clear();
            AllProvidersProcessed = false;

            if (Config == null)
            {
                Log.Debug("Config is not configured yet");
                return;
            }
            
            try
            {
                var crashReportDirectoryPath = new DirectoryInfo(Path.Combine(appArguments.AppDataDirectory, "crashes", $"{appArguments.AppName} {appArguments.Version}{(appArguments.IsDebugMode ? " DEBUG" : string.Empty)} Id{CurrentProcessId} {clock.Now.ToString($"yyyy-MM-dd HHmmss")}"));
                if (crashReportDirectoryPath.Exists)
                {
                    Log.Warn($"Removing existing directory with crash data {crashReportDirectoryPath.FullName}");
                    crashReportDirectoryPath.Delete(true);
                }

                Log.Debug($"Creating directory {crashReportDirectoryPath.FullName}");
                crashReportDirectoryPath.Create();

                if (Config.Exception != null)
                {
                    TryToFormatException(crashReportDirectoryPath, reportItems, Config.Exception);
                }

                var providerIdx = 0;
                foreach (var reportItemProvider in Config.ItemProviders)
                {
                    providerIdx++;

                    try
                    {
                        Log.Debug($"Getting report item from {reportItemProvider}");
                        Status = $"Preparing report {providerIdx}/{Config.ItemProviders.Length}...";

                        var reportItem = reportItemProvider.Prepare(crashReportDirectoryPath);
                        reportItems.AddRange(reportItem);
                        Log.Debug($"Successfully received report item from {reportItemProvider}: {reportItem}");
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
                AllProvidersProcessed = true;
            }
        }
    }
}