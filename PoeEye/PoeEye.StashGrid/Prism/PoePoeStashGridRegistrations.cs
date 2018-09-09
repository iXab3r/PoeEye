using PoeEye.StashGrid.Services;
using PoeEye.StashGrid.ViewModels;
using Unity;
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