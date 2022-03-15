using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Scaffolding;
using Unity.Extension;

namespace PoeShared.Prism;

public sealed class HttpClientRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        Container.RegisterSingleton<IHttpClientFactory>(_ => serviceProvider.GetService<IHttpClientFactory>());
    }
}