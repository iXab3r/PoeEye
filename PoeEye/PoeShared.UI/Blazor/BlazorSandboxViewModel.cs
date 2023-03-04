using System.Windows.Input;
using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    public BlazorHostViewModel WebViewHost { get; }

    public BlazorSandboxViewModel(
        BlazorHostViewModel host,
        IWebViewInstallerDisplayer webViewInstallerDisplayer)
    {
        WebViewHost = host.AddTo(Anchors);
        WebViewHost.Components.Add(typeof(MainCounter));
        
        ShowInstaller = CommandWrapper.Create(() => webViewInstallerDisplayer.ShowDialog(new WebViewInstallerArgs()));
    }
    
    public ICommand ShowInstaller { get; }
}