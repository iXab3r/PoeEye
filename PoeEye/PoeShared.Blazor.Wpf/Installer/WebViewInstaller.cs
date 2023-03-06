using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Core;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf.Installer;

internal sealed class WebViewInstaller : DisposableReactiveObjectWithLogger
{
    private static readonly Binder<WebViewInstaller> Binder = new();

    static WebViewInstaller()
    {
        Binder.Bind(x => x.DownloadAndInstallCommand.IsBusy || x.RefreshCommand.IsBusy).To(x => x.IsBusy);
        Binder.Bind(x => x.webViewAccessor.IsInstalled).To(x => x.IsInstalled);
        Binder.Bind(x => x.webViewAccessor.InstallType).To(x => x.BrowserInstallType);
        Binder.Bind(x => x.webViewAccessor.AvailableBrowserVersion).To(x => x.BrowserVersion);
    }

    private readonly WebViewInstallerArgs args;
    private readonly IWindowViewController viewController;
    private readonly IWebViewAccessor webViewAccessor;
    private readonly IFileDownloader fileDownloader;

    public WebViewInstaller(
        WebViewInstallerArgs args,
        IWindowViewController viewController,
        IWebViewAccessor webViewAccessor,
        IFileDownloader fileDownloader)
    {
        this.args = args;
        this.viewController = viewController;
        this.webViewAccessor = webViewAccessor;
        this.fileDownloader = fileDownloader;
        RefreshCommand = CommandWrapper.Create(Refresh);

        DownloadAndInstallCommand = CommandWrapper.Create(DownloadAndInstall, this.WhenAnyValue(x => x.IsInstalled).Select(x => true));
        CloseWindow = CommandWrapper.Create(viewController.Close);
        Refresh();
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsInstalled { get; [UsedImplicitly] private set; }
    
    public bool IsBusy { get; [UsedImplicitly] private set; }
    
    public CommandWrapper RefreshCommand { get; }
    
    public CommandWrapper DownloadAndInstallCommand { get; }
    
    public CommandWrapper CloseWindow { get; }
    
    public string BrowserVersion { get; [UsedImplicitly] private set; }
    
    public WebViewInstallType BrowserInstallType { get; [UsedImplicitly] private set; }

    public Uri DownloadLink { get; } = new("https://go.microsoft.com/fwlink/p/?LinkId=2124703", UriKind.Absolute);

    public async Task DownloadAndInstall()
    {
        await Task.Delay(UiConstants.ArtificialVeryShortDelay);
        var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder);
        }

        try
        {
            Directory.CreateDirectory(tempFolder);
            var installerPath = new FileInfo(Path.Combine(tempFolder, "MicrosoftEdgeWebview2Setup.exe"));
            Log.Debug(() => $"Downloading installer to {installerPath.FullName} from {DownloadLink}");
            await fileDownloader.DownloadFile(DownloadLink.ToString(), installerPath.FullName, progressPercent => { });
            installerPath.Refresh();
            Log.Debug(() => $"Downloaded installer to {installerPath.FullName}, exists: {installerPath.Exists}");
            if (!installerPath.Exists)
            {
                throw new InvalidStateException($"Could not download WebView2 installer from {DownloadLink}");
            }
            Log.Debug(() => $"Executing installer {installerPath.FullName}, size: {new ByteSizeLib.ByteSize(installerPath.Length)}");

            var result = await Task.Run(() => ProcessHelper.RunCmd(new ProcessStartInfo()
            {
                UseShellExecute = true,
                Arguments = "",
                FileName = installerPath.FullName
            }));
            if (result.ExitCode != 0)
            {
                throw new InvalidStateException($"Installed returned ExitCode: {result.ExitCode} which indicates installation failure");
            }
            Refresh();
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }

    public void Refresh()
    {
        WebViewAccessor.Instance.Refresh();
    }
}