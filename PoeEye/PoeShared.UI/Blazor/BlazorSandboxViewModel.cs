using System;
using System.Windows.Input;
using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    private static readonly Binder<BlazorSandboxViewModel> Binder = new();
    public IWebViewAccessor WebViewAccessor { get; }

    public MainCounterViewModel MainCounter { get; }


    static BlazorSandboxViewModel()
    {
        Binder.Bind(x => !x.MainCounterAlt ? typeof(MainCounterViewAlt) : typeof(MainCounterView)).To(x => x.MainCounterViewType);
    }


    public BlazorSandboxViewModel(
        IWebViewInstallerDisplayer webViewInstallerDisplayer,
        IWebViewAccessor webViewAccessor,
        MainCounterViewModel mainCounter)
    {
        WebViewAccessor = webViewAccessor;
        MainCounter = mainCounter.AddTo(Anchors);

        ShowInstaller = CommandWrapper.Create(() => webViewInstallerDisplayer.ShowDialog(new WebViewInstallerArgs()));

        Binder.Attach(this).AddTo(Anchors);
    }

    public ICommand ShowInstaller { get; }

    public Type MainCounterViewType { get; private set; }

    public bool MainCounterAlt { get; set; }
}