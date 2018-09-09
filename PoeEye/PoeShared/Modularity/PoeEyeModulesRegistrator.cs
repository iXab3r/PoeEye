using System.Collections.ObjectModel;
using Guards;
using Unity;

namespace PoeShared.Modularity
{
    internal sealed class PoeEyeModulesRegistrator : IPoeEyeModulesRegistrator, IPoeEyeModulesEnumerator
    {
        private readonly IUnityContainer container;

        private readonly ObservableCollection<ISettingsViewModel> settings = new ObservableCollection<ISettingsViewModel>();

        public PoeEyeModulesRegistrator(IUnityContainer container)
        {
            Guard.ArgumentNotNull(container, nameof(container));

            this.container = container;
            Settings = new ReadOnlyObservableCollection<ISettingsViewModel>(settings);
        }


        public IPoeEyeModulesRegistrator RegisterSettingsEditor<TConfig, TSettingsViewModel>() 
            where TConfig : class, IPoeEyeConfig, new() 
            where TSettingsViewModel : ISettingsViewModel<TConfig>
        {
            var viewModel = (ISettingsViewModel) container.Resolve(typeof(TSettingsViewModel));
            settings.Add(viewModel);

            return this;
        }

        public ReadOnlyObservableCollection<ISettingsViewModel> Settings { get; } 
    }
}
