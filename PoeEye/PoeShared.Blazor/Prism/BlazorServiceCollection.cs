using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoeShared.Logging;

namespace PoeShared.Blazor.Prism;

public sealed class BlazorServiceCollection : ServiceCollection
{
    private static readonly Lazy<BlazorServiceCollection> InstanceSupplier = new();

    public static BlazorServiceCollection Instance => InstanceSupplier.Value;

    public BlazorServiceCollection()
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