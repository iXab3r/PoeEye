using Microsoft.Practices.Unity;
using PoeEye.StashGrid.Services;
using PoeEye.StashGrid.ViewModels;
using PoeShared.Scaffolding;

namespace PoeEye.StashGrid.Prism
{
    internal sealed class PoePoeStashGridRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IPoeStashHighlightService, PoeStashGridViewModel>();
        }
    }
}