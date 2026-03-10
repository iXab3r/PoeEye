using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Defines extension points for configuring and initializing a <see cref="BlazorContentControl"/>.
/// Implement this interface to hook into the control's lifecycle stages such as
/// configuration start, service registration, and post-initialization.
/// </summary>
public interface IBlazorContentControlConfigurator
{
    /// <summary>
    /// Called at the very beginning of the control's initialization process.
    /// This method is invoked before any services are registered or any containers are built.
    /// It can be used to configure control-level state or prepare external inputs.
    /// </summary>
    /// <returns>A task that completes when the configuration step has finished.</returns>
    Task OnConfiguringAsync();

    /// <summary>
    /// Called during the service registration phase. This allows the configurator to
    /// register additional services into the <see cref="IServiceCollection"/> that will be used
    /// to construct the Blazor DI container.
    /// </summary>
    /// <param name="serviceCollection">The service collection to which additional services can be added.</param>
    /// <returns>A task that completes when service registration is done.</returns>
    Task OnRegisteringServicesAsync(IServiceCollection serviceCollection);

    /// <summary>
    /// Called after the <see cref="IServiceProvider"/> has been fully constructed and assigned
    /// to the control. This phase is appropriate for resolving services, accessing runtime state,
    /// and performing post-configuration logic.
    /// </summary>
    /// <param name="serviceProvider">The final DI container built for the Blazor control.</param>
    /// <returns>A task that completes when post-initialization is complete.</returns>
    Task OnInitializedAsync(IServiceProvider serviceProvider);
}