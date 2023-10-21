using PoeShared.Blazor.Services;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeShared.Blazor.Prism;

public sealed class BlazorWebExtensions : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.RegisterSingleton<BlazorViewRepository>(typeof(IBlazorViewRepository), typeof(IBlazorViewRegistrator));
        BlazorServiceCollection.Instance.AddBlazorRepository(Container);
    }
}