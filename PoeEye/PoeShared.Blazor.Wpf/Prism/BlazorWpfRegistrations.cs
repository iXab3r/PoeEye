using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Wpf.Installer;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container
            .RegisterType<IWebViewInstaller, WebViewInstaller>()
            .RegisterType<IWebViewInstallerWindow, WebViewInstallerWindow>();
        
        Container.RegisterSingleton<IWebViewInstallerDisplayer, WebViewInstallerDisplayer>();
        Container.RegisterSingleton<IWebViewAccessor>(x => WebViewAccessor.Instance);
    }
}