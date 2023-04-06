using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Core;
using PoeShared.UI;
using PropertyBinder;

namespace PoeShared.Blazor.Wpf.Installer;

internal sealed class WebViewInstaller : DisposableReactiveObjectWithLogger, IWebViewInstaller
{
    private readonly IFileDownloader fileDownloader;
    private static readonly Binder<WebViewInstaller> Binder = new();

    static WebViewInstaller()
    {
    }

    public WebViewInstaller(
        IWebViewAccessor webViewAccessor,
        IFileDownloader fileDownloader)
    {
        WebViewAccessor = webViewAccessor;
        this.fileDownloader = fileDownloader;
        WebViewAccessor.Refresh();
        
        Binder.Attach(this).AddTo(Anchors);
    }
    
    public IWebViewAccessor WebViewAccessor { get; }

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
            await fileDownloader.DownloadFile(DownloadLink.ToString(), installerPath.FullName, progressPercent =>
            {
                Log.Info($"Downloading installer from {DownloadLink}... {progressPercent}%");
            });
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
            WebViewAccessor.Refresh();
            if (!WebViewAccessor.IsInstalled)
            {
                throw new InvalidStateException($"Failed to install from file {installerPath.FullName} - WebView is not discovered after installation");
            }
        }
        finally
        {
            Directory.Delete(tempFolder, recursive: true);
        }
    }
}