using System;
using Microsoft.Extensions.DependencyInjection;

namespace PoeShared.Blazor.Wpf;

internal sealed class BlazorServiceCollection : ServiceCollection
{
    private static readonly Lazy<BlazorServiceCollection> InstanceSupplier = new();

    public static BlazorServiceCollection Instance => InstanceSupplier.Value;
}