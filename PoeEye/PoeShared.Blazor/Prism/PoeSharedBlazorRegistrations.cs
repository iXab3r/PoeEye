using System.Reactive.PlatformServices;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Prism;

public sealed class PoeSharedBlazorRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.RegisterSingleton<IServiceCollection>(x => UnityServiceCollection.Instance);
        UnityServiceCollection.Instance.AddSingleton<IUnityContainer>(Container);
        
        Container.RegisterSingleton<BlazorViewRepository>(typeof(IBlazorViewRepository), typeof(IBlazorViewRegistrator));
        UnityServiceCollection.Instance.AddBlazorRepository(Container);
        
        Container.RegisterSingleton<BlazorContentRepository>(typeof(IBlazorContentRepository));
        UnityServiceCollection.Instance.AddBlazorContentRepository(Container);
        UnityServiceCollection.Instance.AddBlazorUtils(Container);

        Container.RegisterSingleton<ISystemClock, MicrosoftExtensionsSystemClock>();
    }
}