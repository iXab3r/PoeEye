using System.Windows.Input;
using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    public MainCounterViewModel MainCounter { get; }
    public BlazorHostViewModel WebViewHost { get; }

    public BlazorSandboxViewModel(
        BlazorHostViewModel host,
        IWebViewInstallerDisplayer webViewInstallerDisplayer,
        MainCounterViewModel mainCounter)
    {
        MainCounter = mainCounter.AddTo(Anchors);
        WebViewHost = host.AddTo(Anchors);
        WebViewHost.Components.Add(typeof(MainCounterView));
        
        ShowInstaller = CommandWrapper.Create(() => webViewInstallerDisplayer.ShowDialog(new WebViewInstallerArgs()));
    }
    
    public ICommand ShowInstaller { get; }
}