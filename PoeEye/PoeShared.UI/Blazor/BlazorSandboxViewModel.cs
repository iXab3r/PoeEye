using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PropertyBinder;
using Unity;

namespace PoeShared.UI.Blazor;

public sealed class BlazorSandboxViewModel : DisposableReactiveObject
{
    private static readonly Binder<BlazorSandboxViewModel> Binder = new();
    public IWebViewAccessor WebViewAccessor { get; }

    public MainCounterViewModel MainCounter { get; }


    static BlazorSandboxViewModel()
    {
        Binder.Bind(x => x.ViewType == ViewTypeEnum.Main ? typeof(MainCounterView) : x.ViewType == ViewTypeEnum.Alt ? typeof(MainCounterViewAlt) : x.ViewType == ViewTypeEnum.Slow ? typeof(SlowView) : typeof(BrokenView))
            .To(x => x.MainCounterViewType);
    }


    public BlazorSandboxViewModel(
        IWebViewInstallerDisplayer webViewInstallerDisplayer,
        IWebViewAccessor webViewAccessor,
        MainCounterViewModel mainCounter,
        IFactory<IBlazorWindow> blazorWindowFactory)
    {
        WebViewAccessor = webViewAccessor;
        MainCounter = mainCounter.AddTo(Anchors);

        ShowInstaller = CommandWrapper.Create(() => webViewInstallerDisplayer.ShowDialog(new WebViewInstallerArgs()));
        ShowWindow = CommandWrapper.Create(() =>
        {
            var wnd = blazorWindowFactory.Create();
            wnd.ViewType = typeof(MainCounterView);
            wnd.DataContext = new MainCounterViewModel();
            wnd.Title = "Test";
            wnd.Height = 300;
            wnd.Width = 200;
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.Show();
        });
        ShowDialogWindow = CommandWrapper.Create(async () =>
        {
            var wnd = blazorWindowFactory.Create();
            wnd.ViewType = typeof(MainCounterView);
            wnd.DataContext = new MainCounterViewModel();
            wnd.Title = "Test blocking";
            wnd.Height = 600;
            wnd.Width = 800;
            wnd.Padding = new Thickness(0);
            wnd.BorderThickness = new Thickness(5);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.TitleBarDisplayMode = TitleBarDisplayMode.Custom;
            await Task.Run(() => wnd.ShowDialog());
        });

        Binder.Attach(this).AddTo(Anchors);
    }

    public ICommand ShowInstaller { get; }

    public ICommand ShowWindow { get; }

    public ICommand ShowDialogWindow { get; }

    public Type MainCounterViewType { get; private set; }

    public ViewTypeEnum ViewType { get; set; }
}