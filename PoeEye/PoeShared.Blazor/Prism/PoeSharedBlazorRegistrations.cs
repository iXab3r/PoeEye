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
        Container.RegisterSingleton<BlazorContentRepository>(typeof(IBlazorContentRepository));
        Container.RegisterSingleton<ISystemClock, MicrosoftExtensionsSystemClock>();

        Container.AsServiceCollection().AddBlazorUtils(Container);
    }
}