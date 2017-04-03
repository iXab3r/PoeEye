using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Guards;
using Microsoft.Practices.Unity;
using ReactiveUI;

namespace PoeShared.Modularity
{
    internal sealed class PoeEyeModulesRegistrator : IPoeEyeModulesRegistrator, IPoeEyeModulesEnumerator
    {
        private readonly IUnityContainer container;

        public PoeEyeModulesRegistrator(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
        }


        public IPoeEyeModulesRegistrator RegisterSettingsEditor<TConfig, TSettingsViewModel>() 
            where TConfig : class, IPoeEyeConfig, new() 
            where TSettingsViewModel : ISettingsViewModel<TConfig>
        {
            var viewModel = (ISettingsViewModel) container.Resolve(typeof(TSettingsViewModel));
            Settings.Add(viewModel);

            return this;
        }

        public IReactiveList<ISettingsViewModel> Settings { get; } = new ReactiveList<ISettingsViewModel>();
    }
}
