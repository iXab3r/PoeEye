using System;
using System.Threading.Tasks;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf.Installer;

public interface IWebViewInstallerWindow : IDisposableReactiveObject
{
    bool IsBusy { get; }
    IWebViewInstaller WebViewInstaller { get; }
}