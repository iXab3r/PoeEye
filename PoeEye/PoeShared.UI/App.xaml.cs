using System.Windows;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Wpf.Prism;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Prism;
using PoeShared.Wpf.Scaffolding;
using Unity;
using Unity.Lifetime;

namespace PoeShared.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : ApplicationBase
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Container.AddNewExtensionIfNotExists<BlazorWpfExtensions>();
        Container.AddNewExtensionIfNotExists<UpdaterRegistrations>();
        Container.AddNewExtensionIfNotExists<BlazorWebExtensions>();

        Container
            .RegisterSingleton<IConfigProvider, ConfigProviderFromFile>();
            
        var window = new MainWindow();
        Container.RegisterOverlayController();
        var viewController = new WindowViewController(window);
        Container.RegisterInstance<IWindowViewController>(WellKnownWindows.MainWindow, viewController, new ContainerControlledLifetimeManager());
        window.DataContext = Container.Resolve<MainWindowViewModel>();
        window.Show();
    }
}