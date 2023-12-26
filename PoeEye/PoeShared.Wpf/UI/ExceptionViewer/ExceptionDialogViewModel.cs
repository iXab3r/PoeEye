using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Win32;
using PoeShared.Dialogs.Services;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using Syroot.Windows.IO;
using Unity;

namespace PoeShared.UI;

internal sealed class ExceptionDialogViewModel : DisposableReactiveObject
{
    private static readonly IFluentLog Log = typeof(ExceptionDialogViewModel).PrepareLogger();
    private static readonly Binder<ExceptionDialogViewModel> Binder = new();

    private static readonly string ExplorerExecutablePath =
        Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");

    private readonly IAppArguments appArguments;
    private readonly IReportItemsAggregator reportItemsAggregator;
    private readonly IUniqueIdGenerator idGenerator;

    private readonly ISevenZipWrapper sevenZipWrapper;
    private readonly IMessageBoxService messageBoxService;

    static ExceptionDialogViewModel()
    {
        Binder.Bind(x => x.SaveReportCommand.IsBusy || x.SendReportCommand.IsBusy || !x.reportItemsAggregator.IsReady).To(x => x.IsBusy);
        Binder.BindIf(x => x.SelectedItem == null && x.Attachments.Count > 0, x => x.Attachments[0]).To(x => x.SelectedItem);
        Binder.BindIf(x => x.Config != null, x => x.Config.Title).Else(x => default).To(x => x.Title);
        Binder.BindIf(x => x.Config != null, x => x.Config.AppName).Else(x => default).To(x => x.AppName);
        Binder.BindIf(x => x.reportItemsAggregator.Status != null, x => x.reportItemsAggregator.Status).To(x => x.Status);
    }

    public ExceptionDialogViewModel(
        IClipboardManager clipboardManager,
        IAppArguments appArguments, 
        IReportItemsAggregator reportItemsAggregator,
        IUniqueIdGenerator idGenerator,
        ISevenZipWrapper sevenZipWrapper,
        ICloseController closeController,
        IMessageBoxService messageBoxService,
        [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
    {
        this.appArguments = appArguments;
        this.reportItemsAggregator = reportItemsAggregator;
        this.idGenerator = idGenerator;
        this.sevenZipWrapper = sevenZipWrapper;
        this.messageBoxService = messageBoxService;

        this.RaiseWhenSourceValue(x => x.AppName, this, x => x.Config).AddTo(Anchors);
        CloseCommand = CommandWrapper.Create(closeController.Close);
        SaveReportCommand = CommandWrapper.Create(SaveReportCommandExecuted, this.WhenAnyValue(x => x.IsBusy).ObserveOn(uiScheduler).Select(x => x == false));
        SendReportCommand = CommandWrapper.Create(SendReportCommandExecuted, 
            Observable.CombineLatest(
                this.WhenAnyValue(x => x.Config).Select(x => x?.ReportHandler != null),
                this.WhenAnyValue(x => x.SentReportId),
                this.WhenAnyValue(x => x.IsBusy).Select(x => x == false),(hasHandler, sentReportId, notBusy) => hasHandler && notBusy && string.IsNullOrEmpty(sentReportId)).ObserveOn(uiScheduler));
        CopyStatusToClipboard = CommandWrapper.Create(() => clipboardManager.SetText(SentReportId ?? Status), 
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.IsBusy).Select(x => x == false),
                    this.WhenAnyValue(x => x.SentReportId),
                    this.WhenAnyValue(x => x.Status), (notBusy, sentReportId, status) => notBusy && (!string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(sentReportId)))
                .ObserveOn(uiScheduler));

        reportItemsAggregator
            .ReportItems
            .Connect()
            .Filter(x => x.IsRequired == false)
            .Transform(x => new ExceptionDialogSelectableItem(x))
            .Sort(new SortExpressionComparer<ExceptionDialogSelectableItem>().ThenByDescending(x => x.IsChecked))
            .ObserveOn(uiScheduler)
            .Bind(out var attachments)
            .SubscribeToErrors(Log.HandleException)
            .AddTo(Anchors);
        Attachments = attachments;
            
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
    
    public string SentReportId { get; private set; }

    public string Status { get; private set; }

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
            Log.Debug($"Report sent to {Config.ReportHandler}: {result}");
            SentReportId = result;
            Status = $"Report sent";
            await messageBoxService.ShowMessage("Report sent successfully", $"Your report has Id {result}. Feel free to send this Id via Discord or by any other means if you consider this bug important, this will speed up processing process");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to send report to {Config.ReportHandler}", ex);
            Status = $"Failed to send report - {ex.Message}";
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
        return $"{appArguments.AppName}_{appArguments.Version}_{appArguments.Profile}_{idGenerator.Next()}.7z";
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
        
        reportItemsAggregator.ReportItems.Items.Where(x => x.IsRequired)
            .Select(x => x.Attachment.FullName)
            .ForEach(filesToAttach.Add);

        Attachments
            .Where(x => x.IsChecked)
            .Select(x => x.Item.Attachment.FullName)
            .ForEach(filesToAttach.Add);

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
            Log.Debug($"Compressing report with following files: {filesToAttach.DumpToString()}");
            sevenZipWrapper.AddToArchive(outputFile, filesToAttach.Select(x => new FileInfo(x)).ToArray());
            Log.Debug($"Compression has completed");
        });
            
        outputFile.Refresh();
        Log.Debug($"Compressed directory {outputDirectory} as {outputFile.FullName} ({outputFile.Length}b)");
    }

    
}