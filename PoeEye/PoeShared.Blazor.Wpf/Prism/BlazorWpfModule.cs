using System.ComponentModel;
using PoeShared.Blazor.Prism;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Prism.Ioc;
using Prism.Unity;
using Unity;

namespace PoeShared.Blazor.Wpf.Prism;

public sealed class BlazorWpfModule : DynamicModule
{
    protected override void RegisterTypesInternal(IUnityContainer container)
    {
        container.AddNewExtensionIfNotExists<BlazorWpfRegistrations>();
        container.AddNewExtensionIfNotExists<PoeSharedBlazorRegistrations>();
    }
}