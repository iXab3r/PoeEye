namespace PoeShared.Blazor.Wpf.Installer;

public interface IWebViewInstallerDisplayer
{
    bool? ShowDialog(WebViewInstallerArgs args);
}