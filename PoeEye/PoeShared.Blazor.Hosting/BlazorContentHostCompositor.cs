using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using Unity;

namespace PoeShared.Blazor.Wpf;

internal static class BlazorContentHostCompositor
{
    public static async Task ComposeAsync(BlazorContentHostCompositionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var serviceCollection = context.ChildContainer.AsServiceCollection();
        context.RegisterHostServices(serviceCollection);

        context.Log.Debug("Notifying visitor that registration stage is ongoing");
        await context.Configurator.OnRegisteringServicesAsync(serviceCollection);

        var childServiceProvider = BlazorContentHostUtilities.BuildUnityServiceProvider(context.ChildContainer, serviceCollection);
        childServiceProvider.GetRequiredService<IClock>();

        using (var tempScope = childServiceProvider.CreateScope())
        {
            tempScope.ServiceProvider.GetRequiredService<IClock>();
        }

        context.WebViewServiceProvider.ServiceProvider = childServiceProvider;

        var globalMemoryFileProvider = new InMemoryFileProvider();

        // static web assets file provider is created by WebView manager, and we cannot directly access it from outside
        // thus we create our own, still prioritizing the usual one - this gives user the ability to replace assets
        var publicInMemoryFileProvider = new InMemoryFileProvider();
        serviceCollection.AddSingleton<IInMemoryFileProvider>(_ => publicInMemoryFileProvider);
        serviceCollection.AddScoped<IJSComponentConfiguration>(_ => context.RootComponents);

        var proxyFileProvider = new ProxyFileProvider
        {
            FileProvider = context.AdditionalFileProvider
        };

        var rootFileProvider = context.WebViewServiceProvider.ServiceProvider.GetRequiredService<IRootContentFileProvider>();
        var webViewFileProvider = new ReactiveCompositeFileProvider(
            publicInMemoryFileProvider,
            proxyFileProvider,
            globalMemoryFileProvider,
            rootFileProvider);

        context.SetWebViewFileProvider(webViewFileProvider);

        var blazorContentRepository = context.WebViewServiceProvider.ServiceProvider.GetRequiredService<IBlazorContentRepository>();
        var repositoryAdditionalFiles = blazorContentRepository.AdditionalFiles.Items.ToArray();
        var controlAdditionalFiles = context.AdditionalFiles ?? Array.Empty<IFileInfo>();
        var additionalFiles = repositoryAdditionalFiles.Concat(controlAdditionalFiles).ToArray();
        if (additionalFiles.Any())
        {
            context.Log.Debug($"Loading additional files({additionalFiles.Length}):\n\t{additionalFiles.Select(x => x.Name).DumpToTable()}");
            foreach (var file in additionalFiles)
            {
                if (file is RefFileInfo)
                {
                    continue;
                }

                globalMemoryFileProvider.FilesByName.Edit(updater => updater.AddOrUpdate(file));
            }
        }

        var indexFileContentTemplate = webViewFileProvider.ReadAllText(context.IndexFileSubpath);
        var indexFileContent = BlazorContentHostUtilities.PrepareIndexFileContext(indexFileContentTemplate, additionalFiles);
        publicInMemoryFileProvider.FilesByName.Edit(updater => updater.AddOrUpdate(new InMemoryFileInfo(context.GeneratedIndexFileName, Encoding.UTF8.GetBytes(indexFileContent), DateTimeOffset.Now)));

        var jsComponentsAccessor = new JSComponentConfigurationStoreAccessor(blazorContentRepository.JSComponents);
        var webRootComponentsAccessor = new JSComponentConfigurationStoreAccessor(context.RootComponentsStore);
        foreach (var kvp in jsComponentsAccessor.JsComponentTypesByIdentifier)
        {
            if (webRootComponentsAccessor.JsComponentTypesByIdentifier.ContainsKey(kvp.Key))
            {
                continue;
            }

            context.Log.Debug($"Registering RootComponent: {kvp}");
            webRootComponentsAccessor.RegisterForJavaScript(kvp.Value, kvp.Key);
        }

        context.Log.Debug("Notifying visitor everything is up and ready for work");
        await context.Configurator.OnInitializedAsync(childServiceProvider);

        if (string.Equals(context.GetCurrentHostPage(), context.HostPage, StringComparison.Ordinal) && context.IsWebViewReady())
        {
            context.Log.Debug($"Reloading existing page, view type: {context.State}");
            await context.ReloadCurrentPage();
        }
        else
        {
            context.Log.Debug($"Navigating to index page, view type: {context.State}");
            context.SetHostPage(context.HostPage);
        }
    }
}

internal sealed class BlazorContentHostCompositionContext
{
    public required IUnityContainer ChildContainer { get; init; }

    public required IBlazorContentControlConfigurator Configurator { get; init; }

    public required WebViewServiceProvider WebViewServiceProvider { get; init; }

    public required IJSComponentConfiguration RootComponents { get; init; }

    public required JSComponentConfigurationStore RootComponentsStore { get; init; }

    public required string IndexFileSubpath { get; init; }

    public required string GeneratedIndexFileName { get; init; }

    public required string HostPage { get; init; }

    public required IFluentLog Log { get; init; }

    public required object State { get; init; }

    public IFileProvider? AdditionalFileProvider { get; init; }

    public IReadOnlyList<IFileInfo>? AdditionalFiles { get; init; }

    public required Action<IServiceCollection> RegisterHostServices { get; init; }

    public required Action<IFileProvider> SetWebViewFileProvider { get; init; }

    public required Func<string?> GetCurrentHostPage { get; init; }

    public required Func<bool> IsWebViewReady { get; init; }

    public required Func<Task> ReloadCurrentPage { get; init; }

    public required Action<string> SetHostPage { get; init; }
}
