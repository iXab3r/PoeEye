using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfRegistrations : UnityContainerExtension
{
    private static readonly IFluentLog Log = typeof(BlazorWpfRegistrations).PrepareLogger();

    protected override void Initialize()
    {
        Container
            .RegisterType<IBlazorWindow, BlazorWindow>();
        
        Container.RegisterSingleton<IStaticWebAssetsFileProvider, StaticWebAssetsFileProvider>();
        Container.RegisterSingleton<IWebViewAccessor>(x => WebViewAccessor.Instance);
        Container.RegisterSingleton<IRootContentFileProvider, RootContentFileProvider>();
        
        UnityServiceCollection.Instance.AddContextMenuService(Container);
    }
}