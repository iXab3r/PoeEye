using PoeShared.Blazor;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Controls.Services;
using Unity;
using PoeShared.Scaffolding;
using Unity.Extension;

namespace PoeShared.Blazor.Controls.Prism;

public sealed class PoeSharedBlazorControlsRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.AsServiceCollection().AddPoeSharedBlazorControls();
        if (Container.IsRegistered<IBlazorContentRepository>())
        {
            Container.Resolve<IBlazorContentRepository>().AddReactiveCollectionComponents();
        }
    }
}
