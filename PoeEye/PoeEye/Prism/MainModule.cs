using Guards;
using Microsoft.Practices.Unity;
using PoeEye.Config;
using PoeEye.PoeTrade.ViewModels;
using PoeShared.Modularity;

namespace PoeEye.Prism
{
    internal sealed class MainModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public MainModule(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }

        public void Initialize()
        {
            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeEyeMainConfig, PoeMainSettingsViewModel>();
        }
    }
}
