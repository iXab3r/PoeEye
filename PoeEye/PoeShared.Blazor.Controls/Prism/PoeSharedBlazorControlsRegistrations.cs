using PoeShared.Blazor.Prism;
using Unity.Extension;

namespace PoeShared.Blazor.Controls.Prism;

public sealed class PoeSharedBlazorControlsRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        UnityServiceCollection.Instance.AddPoeSharedBlazorControls();
    }
}