using System.Windows.Input;
using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    public IWebViewAccessor WebViewAccessor { get; }
    public MainCounterViewModel MainCounter { get; }

    public BlazorSandboxViewModel(
        IWebViewInstallerDisplayer webViewInstallerDisplayer,
        IWebViewAccessor webViewAccessor,
        MainCounterViewModel mainCounter)
    {
        WebViewAccessor = webViewAccessor;
        MainCounter = mainCounter.AddTo(Anchors);
        
        ShowInstaller = CommandWrapper.Create(() => webViewInstallerDisplayer.ShowDialog(new WebViewInstallerArgs()));
    }
    
    public ICommand ShowInstaller { get; }
}