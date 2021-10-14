using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Microsoft.Win32;
using PoeShared.Scaffolding;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;
using SevenZip;
using Syroot.Windows.IO;

namespace PoeShared.UI
{
    internal sealed class ExceptionDialogViewModel : DisposableReactiveObject
    {
        private static readonly IFluentLog Log = typeof(ExceptionDialogViewModel).PrepareLogger();

        private static readonly string ExplorerExecutablePath =
            Environment.ExpandEnvironmentVariables(@"%WINDIR%\explorer.exe");
        
        private readonly SourceList<ExceptionDialogSelectableItem> attachementsSource = new();
        private readonly IClock clock;
        private readonly IAppArguments appArguments;
        private readonly ICloseController closeController;

        public ExceptionDialogViewModel(
            IClock clock,
            IAppArguments appArguments,
            ICloseController closeController)
        {
            this.clock = clock;
            this.appArguments = appArguments;
            this.closeController = closeController;

            this.RaiseWhenSourceValue(x => x.Title, this, x => x.Config).AddTo(Anchors);
            this.RaiseWhenSourceValue(x => x.AppName, this, x => x.Config).AddTo(Anchors);
            CloseCommand = CommandWrapper.Create(closeController.Close);
            PrepareReportCommand = CommandWrapper.Create(PrepareReportCommandExecuted);

            attachementsSource
                .Connect()
                .Bind(out var attachments)
                .Subscribe()
                .AddTo(Anchors);
            Attachments = attachments;

            this.WhenAnyValue(x => x.Config)
                .Subscribe(x =>
                {
                    attachementsSource.Clear();
                    x?.FilesToAttach?.EmptyIfNull().Select(x => new ExceptionDialogSelectableItem(x)).ForEach(attachementsSource.Add);
                    SelectedItem = attachments.FirstOrDefault();
                })
                .AddTo(Anchors);
        }

        public ExceptionDialogConfig Config { get; set; }

        public ReadOnlyObservableCollection<ExceptionDialogSelectableItem> Attachments { get; }

        public string Title => Config?.Title;

        public string AppName => Config?.AppName;

        public string Comment { get; set; }

        public ExceptionDialogSelectableItem SelectedItem { get; set; }

        public ICommand CloseCommand { get; }

        public CommandWrapper PrepareReportCommand { get; }

        public string LastSavedFile { get; set; }

        private async Task PrepareReportCommandExecuted()
        {
            var initialDirectory = Path.GetDirectoryName(LastSavedFile);
            var defaultFileName = $"{appArguments.AppName} {appArguments.Version}{(appArguments.IsDebugMode ? " DEBUG" : string.Empty)} {clock.Now.ToString($"yyyy-MM-dd HHmmss")}";

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
                return;
            }

            LastSavedFile = op.FileName;

            var filesToAttach = new List<string>();
            Attachments.Where(x => x.IsChecked).Select(x => x.Item.Attachment.FullName).ForEach(filesToAttach.Add);

            var commonDir = filesToAttach.Select(x => Path.GetDirectoryName(x) ?? string.Empty).Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .FirstOrDefault();
            
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            using var removeOnClose = Disposable.Create(() => Directory.Delete(tempDirectory, true));

            if (!string.IsNullOrEmpty(Comment))
            {
                var fileWithComment = Path.Combine(tempDirectory, "comment.txt");
                await File.WriteAllTextAsync(fileWithComment, Comment);
                filesToAttach.Add(fileWithComment);
            }

            await Task.Run(() =>
            {
                var newArchive = new SevenZipCompressor();
                Log.Debug($"Compressing report with following files: {filesToAttach.DumpToTable()}");
                newArchive.DirectoryStructure = false;
                newArchive.CompressionLevel = CompressionLevel.Normal;
                newArchive.CompressFiles(op.FileName, filesToAttach.ToArray());
            });
            
            Log.Debug("Compression completed, opening Explorer");
            
            Log.Debug($"Opening link: {op.FileName}");
            var process = Process.Start(ExplorerExecutablePath, $"/select,\"{op.FileName}\"");
            if (process == null)
            {
                throw new ApplicationException("Failed to open Explorer");
            }
            await process.WaitForExitAsync();
        }
    }
}