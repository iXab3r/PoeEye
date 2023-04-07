using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Wpf.Installer;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfExtensions : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container
            .RegisterType<IWebViewInstaller, WebViewInstaller>()
            .RegisterType<IWebViewInstallerWindow, WebViewInstallerWindow>();
        
        Container.RegisterSingleton<IWebViewInstallerDisplayer, WebViewInstallerDisplayer>();
        Container.RegisterFactory<IServiceCollection>(x => BlazorServiceCollection.Instance);
        
        Container.RegisterFactory<IWebViewAccessor>(x => WebViewAccessor.Instance);
    }
}