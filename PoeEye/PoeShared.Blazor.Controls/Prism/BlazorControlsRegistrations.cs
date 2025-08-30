using PoeShared.Blazor.Prism;
using Unity.Extension;

namespace PoeShared.Blazor.Controls.Prism;

public sealed class BlazorControlsRegistrations : UnityContainerExtension
{
    protected override void Initialize()
    {
        UnityServiceCollection.Instance.AddBlazorControls(Container);
    }
}