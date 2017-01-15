using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeChatWheel.Modularity;
using PoeChatWheel.ViewModels;
using PoeShared.Modularity;
using IModule = Prism.Modularity.IModule;

namespace PoeChatWheel.Prism
{
    public sealed class PoeChatWheelModule : IPoeEyeModule
    {
        private readonly IUnityContainer container;

        public PoeChatWheelModule([NotNull] IUnityContainer container)
        {
            Guard.ArgumentNotNull(() => container);

            this.container = container;
        }

        public void Initialize()
        {
            container.AddExtension(new PoeChatWheelRegistrations());

            var registrator = container.Resolve<IPoeEyeModulesRegistrator>();
            registrator.RegisterSettingsEditor<PoeChatWheelConfig, PoeChatWheelSettingsViewModel>();
        }
    }
}