using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Logging;

namespace PoeShared.Blazor.Wpf.Services;

internal sealed class LoggingBlazorContentControlConfigurator : BlazorContentControlConfiguratorBase
{
    public IFluentLog Log { get; }

    public LoggingBlazorContentControlConfigurator(IFluentLog log)
    {
        Log = log;
    }

    public override async Task OnConfiguringAsync()
    {
        Log.Debug($"{nameof(OnConfiguringAsync)} is called");
    }

    public override async Task OnInitializedAsync(IServiceProvider serviceProvider)
    {
        Log.Debug($"{nameof(OnInitializedAsync)} is called, service provider: {serviceProvider}");
    }

    public override async Task OnRegisteringServicesAsync(IServiceCollection serviceCollection)
    {
        Log.Debug($"{nameof(OnRegisteringServicesAsync)} is called, service collection: {serviceCollection} ({serviceCollection.Count} services)");
    }
}