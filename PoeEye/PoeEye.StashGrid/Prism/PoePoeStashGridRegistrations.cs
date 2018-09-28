using PoeEye.StashGrid.Services;
using PoeEye.StashGrid.ViewModels;
using PoeShared.Scaffolding;
using Unity;
using Unity.Extension;

namespace PoeEye.StashGrid.Prism
{
    internal sealed class PoePoeStashGridRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<PoeStashGridViewModel>(typeof(IPoeStashGridViewModel), typeof(IPoeStashHighlightService));
        }
    }
}