using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using Unity.Extension;

namespace PoeShared.Blazor.Prism;

public sealed class BlazorWebRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.RegisterSingleton<BlazorViewRepository>(typeof(IBlazorViewRepository), typeof(IBlazorViewRegistrator));
        BlazorServiceCollection.Instance.AddBlazorRepository(Container);
        
        Container.RegisterSingleton<BlazorContentRepository>(typeof(IBlazorContentRepository));
        BlazorServiceCollection.Instance.AddBlazorContentRepository(Container);
    }
}