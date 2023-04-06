using System;
using System.Threading.Tasks;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Installer;

public interface IWebViewInstaller : IDisposableReactiveObject
{
    IWebViewAccessor WebViewAccessor { get; }
    Uri DownloadLink { get; }
    Task DownloadAndInstall();
}