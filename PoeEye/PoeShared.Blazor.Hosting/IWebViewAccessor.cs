using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

public interface IWebViewAccessor : IDisposableReactiveObject
{
    bool IsInstalled { get; }
    string AvailableBrowserVersion { get; }
    WebViewInstallType InstallType { get; }
    void Refresh();
}