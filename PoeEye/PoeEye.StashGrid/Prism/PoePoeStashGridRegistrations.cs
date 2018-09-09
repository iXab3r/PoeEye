using Unity; using Unity.Resolution; using Unity.Attributes;
using PoeEye.StashGrid.Services;
using PoeEye.StashGrid.ViewModels;
using PoeShared.Scaffolding;
using Unity.Extension;

namespace PoeEye.StashGrid.Prism
{
    internal sealed class PoePoeStashGridRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoeStashGridViewModel, PoeStashGridViewModel>()
                .RegisterSingleton<IPoeStashHighlightService, PoeStashGridViewModel>();
        }
    }
}