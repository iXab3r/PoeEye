using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Base class for implementing <see cref="IBlazorContentControlConfigurator"/> with no-op defaults.
/// 
/// This class is intended to simplify the creation of configurators by allowing implementers 
/// to override only the lifecycle stages they are interested in.
/// </summary>
public abstract class BlazorContentControlVisitorBase : IBlazorContentControlConfigurator
{
    /// <inheritdoc />
    public virtual Task OnConfiguringAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnRegisteringServicesAsync(IServiceCollection serviceCollection)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task OnInitializedAsync(IServiceProvider serviceProvider)
    {
        return Task.CompletedTask;
    }
}