using PoeShared.Blazor.Prism;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using Unity;

namespace PoeShared.Blazor.WinForms.Prism;

public sealed class BlazorWinFormsModule : DynamicModule
{
    protected override void RegisterTypesInternal(IUnityContainer container)
    {
        container.AddNewExtensionIfNotExists<BlazorWinFormsRegistrations>();
        container.AddNewExtensionIfNotExists<PoeSharedBlazorRegistrations>();
    }
}