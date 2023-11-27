using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoeShared.Logging;

namespace PoeShared.Blazor.Prism;

/// <summary>
/// This ServiceCollection is expected to bind IUnityContainer world to .NET Core - it is expected to hold reference to the root IUnityContainer
/// </summary>
public sealed class UnityServiceCollection : ServiceCollection
{
    private static readonly Lazy<UnityServiceCollection> InstanceSupplier = new();

    public static UnityServiceCollection Instance => InstanceSupplier.Value;

    public UnityServiceCollection()
    {
        this.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.ClearProviders();
            builder.AddProvider(new Log4NetLoggerProvider());

            builder.AddFilter("Microsoft.AspNetCore.Components.WebView", LogLevel.Trace);
        });
    }
}