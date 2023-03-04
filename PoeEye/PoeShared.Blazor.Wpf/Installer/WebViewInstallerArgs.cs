using System.Windows;

namespace PoeShared.Blazor.Wpf.Installer;

public sealed record WebViewInstallerArgs
{
    public Window Owner { get; set; }
}