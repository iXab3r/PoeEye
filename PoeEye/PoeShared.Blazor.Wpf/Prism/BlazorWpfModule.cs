using PoeShared.Blazor.Prism;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        var container = containerRegistry.GetContainer();
        container.AddNewExtensionIfNotExists<BlazorWpfRegistrations>();
        container.AddNewExtensionIfNotExists<PoeSharedBlazorRegistrations>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
    }
}
