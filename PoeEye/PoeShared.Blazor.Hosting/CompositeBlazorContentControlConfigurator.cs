using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Reactive composite that aggregates multiple <see cref="IBlazorContentControlConfigurator"/> instances.
/// Allows dynamic addition/removal of configurators and invokes them in registration order.
/// </summary>
public sealed class CompositeBlazorContentControlConfigurator : DisposableReactiveObjectWithLogger, IBlazorContentControlConfigurator
{
    private readonly ISourceList<IBlazorContentControlConfigurator> configuratorsSource = new SourceList<IBlazorContentControlConfigurator>();

    public CompositeBlazorContentControlConfigurator()
    {
        // Dynamically update Configurators on source list change
        configuratorsSource
            .Connect()
            .Subscribe(_ => { Configurators = configuratorsSource.Items.ToImmutableArray(); })
            .AddTo(Anchors);
    }

    /// <summary>
    /// Current snapshot of active configurators.
    /// </summary>
    public ImmutableArray<IBlazorContentControlConfigurator> Configurators { get; private set; } = ImmutableArray<IBlazorContentControlConfigurator>.Empty;

    /// <summary>
    /// Adds a configurator to the composite. Returns a disposable to remove it.
    /// </summary>
    /// <param name="configurator">The configurator to add.</param>
    /// <returns>A disposable that removes the configurator when disposed.</returns>
    public IDisposable Add(IBlazorContentControlConfigurator configurator)
    {
        configuratorsSource.Add(configurator);
        return Disposable.Create(() => { configuratorsSource.Remove(configurator); });
    }

    /// <inheritdoc />
    public async Task OnConfiguringAsync()
    {
        foreach (var configurator in Configurators)
        {
            await configurator.OnConfiguringAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task OnRegisteringServicesAsync(IServiceCollection serviceCollection)
    {
        foreach (var configurator in Configurators)
        {
            await configurator.OnRegisteringServicesAsync(serviceCollection).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task OnInitializedAsync(IServiceProvider serviceProvider)
    {
        foreach (var configurator in Configurators)
        {
            await configurator.OnInitializedAsync(serviceProvider).ConfigureAwait(false);
        }
    }
}