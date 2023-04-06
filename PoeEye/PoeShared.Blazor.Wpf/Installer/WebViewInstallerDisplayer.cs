using System.Windows;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeShared.Blazor.Wpf.Installer;

internal sealed class WebViewInstallerDisplayer : IWebViewInstallerDisplayer
{
    private readonly IFactory<WebViewInstallerWindow, IWindowViewController, WebViewInstallerArgs> webViewInstallerFactory;
    private readonly IFactory<WebViewInstallWindow> windowFactory;

    public WebViewInstallerDisplayer(
        IFactory<WebViewInstallerWindow, IWindowViewController, WebViewInstallerArgs> webViewInstallerFactory,
        IFactory<WebViewInstallWindow> windowFactory)
    {
        this.webViewInstallerFactory = webViewInstallerFactory;
        this.windowFactory = windowFactory;
    }

    public bool? ShowDialog(WebViewInstallerArgs args)
    {
        var window = windowFactory.Create();
        var viewController = new WindowViewController(window);
        window.Owner = args.Owner;
        using var updaterWindowViewModel = webViewInstallerFactory.Create(viewController, args);
        {
            window.WindowStartupLocation = window.Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
            window.DataContext = updaterWindowViewModel;
            return window.ShowDialog();
        }
    }
}