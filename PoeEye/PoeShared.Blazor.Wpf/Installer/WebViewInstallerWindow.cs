using System;
using System.Reactive.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Squirrel.Core;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf.Installer;

internal sealed class WebViewInstallerWindow : DisposableReactiveObjectWithLogger, IWebViewInstallerWindow
{
    private static readonly Binder<WebViewInstallerWindow> Binder = new();

    static WebViewInstallerWindow()
    {
        Binder.Bind(x => x.DownloadAndInstallCommand.IsBusy || x.RefreshCommand.IsBusy).To(x => x.IsBusy);
    }

    private readonly WebViewInstallerArgs args;
    private readonly IWindowViewController viewController;

    public WebViewInstallerWindow(
        WebViewInstallerArgs args,
        IWindowViewController viewController,
        IWebViewInstaller webViewInstaller)
    {
        this.args = args;
        this.viewController = viewController;
        WebViewInstaller = webViewInstaller;
        RefreshCommand = CommandWrapper.Create(WebViewInstaller.WebViewAccessor.Refresh);

        DownloadAndInstallCommand = CommandWrapper.Create(webViewInstaller.DownloadAndInstall, this.WhenAnyValue(x => x.WebViewInstaller.WebViewAccessor.IsInstalled).Select(x => true));
        CloseWindow = CommandWrapper.Create(viewController.Close);
        Binder.Attach(this).AddTo(Anchors);
    }

    
    public bool IsBusy { get; [UsedImplicitly] private set; }

    public CommandWrapper RefreshCommand { get; }
    
    public CommandWrapper DownloadAndInstallCommand { get; }
    
    public CommandWrapper CloseWindow { get; }
    
    public IWebViewInstaller WebViewInstaller { get; }

   
}