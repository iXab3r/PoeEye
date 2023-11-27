using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Prism;

public sealed class BlazorWebRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.RegisterFactory<IServiceCollection>(x => UnityServiceCollection.Instance);
        UnityServiceCollection.Instance.AddSingleton<IUnityContainer>(Container);
        
        Container.RegisterSingleton<BlazorViewRepository>(typeof(IBlazorViewRepository), typeof(IBlazorViewRegistrator));
        UnityServiceCollection.Instance.AddBlazorRepository(Container);
        
        Container.RegisterSingleton<BlazorContentRepository>(typeof(IBlazorContentRepository));
        UnityServiceCollection.Instance.AddBlazorContentRepository(Container);
    }
}