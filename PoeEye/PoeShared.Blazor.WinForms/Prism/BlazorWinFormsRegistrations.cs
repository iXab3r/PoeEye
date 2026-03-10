using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Services;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Blazor.WinForms;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.WinForms.Prism;

public sealed class BlazorWinFormsRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.RegisterType<IBlazorWindow, BlazorWindow>();
        Container.RegisterSingleton<IStaticWebAssetsFileProvider, StaticWebAssetsFileProvider>();
        Container.RegisterSingleton<IWebViewAccessor>(_ => WebViewAccessor.Instance);
        Container.RegisterSingleton<IRootContentFileProvider, RootContentFileProvider>();

        Container.AsServiceCollection().AddScoped<IBlazorContextMenuService, WebView2ContextMenuService>();
    }
}
