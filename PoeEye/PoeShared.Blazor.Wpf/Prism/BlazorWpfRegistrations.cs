using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Wpf.Automation;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;
using Unity.Injection;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfRegistrations : UnityContainerExtension
{
    /// <summary>
    /// Identifies the desktop application's main native window for automatic ownership when no application window
    /// is active. Called once by the host; registration is weak and does not create or activate the window.
    /// </summary>
    public void ConfigureMainWindow(INativeWindow window)
    {
        NativeWindowRegistry.Instance.SetMainWindow((NativeWindow)window);
    }

    private static readonly IFluentLog Log = typeof(BlazorWpfRegistrations).PrepareLogger();

    protected override void Initialize()
    {
        ConfigureActivationSuppression(false);
        Container.RegisterSingleton<IBlazorWindowViewRegistry, BlazorWindowViewRegistry>();
        Container.RegisterSingleton<IBlazorWindowViewRegistryRegistrar>(x => (BlazorWindowViewRegistry) x.Resolve<IBlazorWindowViewRegistry>());
        Container.RegisterSingleton<IWebView2EnvironmentController, WebView2EnvironmentController>();
        Container.RegisterSingleton<IBlazorWebViewAutomationOptionsProvider, DefaultBlazorWebViewAutomationOptionsProvider>();
        
        Container.RegisterSingleton<IStaticWebAssetsFileProvider, StaticWebAssetsFileProvider>();
        Container.RegisterSingleton<IWebViewAccessor>(x => WebViewAccessor.Instance);
        Container.RegisterSingleton<IRootContentFileProvider, RootContentFileProvider>();
        
        Container.AsServiceCollection().AddWpfContextMenuService(Container);
    }

    /// <summary>
    /// Sets the activation-suppression default for subsequently resolved native and Blazor windows.
    /// Call during host setup, before resolving windows. Property injection leaves scoped Blazor
    /// configurators intact and completes before a window is returned to its caller.
    /// </summary>
    public void ConfigureActivationSuppression(bool suppressActivation)
    {
        Container.RegisterType<IWpfBlazorWindow, BlazorWindow>(
            new InjectionProperty(nameof(INativeWindow.SuppressActivation), suppressActivation));
        Container.RegisterType<IBlazorWindow, BlazorWindow>(
            new InjectionProperty(nameof(INativeWindow.SuppressActivation), suppressActivation));
        Container.RegisterType<INativeWindow, NativeWindow>(
            new InjectionProperty(nameof(INativeWindow.SuppressActivation), suppressActivation));
    }
}
