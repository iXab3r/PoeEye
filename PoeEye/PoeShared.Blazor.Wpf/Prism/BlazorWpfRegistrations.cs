using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Wpf.Installer;
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
            .RegisterType<IWebViewInstaller, WebViewInstaller>()
            .RegisterType<IBlazorWindow, BlazorWindow>()
            .RegisterType<IBlazorWindowViewController, BlazorWindowViewController>()
            .RegisterType<IWebViewInstallerWindow, WebViewInstallerWindow>();
        
        Container.RegisterSingleton<IStaticWebAssetsFileProvider, StaticWebAssetsFileProvider>();
        Container.RegisterSingleton<IWebViewInstallerDisplayer, WebViewInstallerDisplayer>();
        Container.RegisterSingleton<IWebViewAccessor>(x => WebViewAccessor.Instance);
        Container.RegisterSingleton<IRootContentFileProvider, RootContentFileProvider>();
    }
}