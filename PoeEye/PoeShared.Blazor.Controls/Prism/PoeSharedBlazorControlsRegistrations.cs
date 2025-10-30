using PoeShared.Blazor.Prism;
using PoeShared.Scaffolding;
using Unity.Extension;

namespace PoeShared.Blazor.Controls.Prism;

public sealed class PoeSharedBlazorControlsRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        Container.AsServiceCollection().AddPoeSharedBlazorControls();
    }
}