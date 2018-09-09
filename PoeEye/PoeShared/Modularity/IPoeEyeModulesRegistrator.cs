using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace PoeShared.Modularity
{
    public interface IPoeEyeModulesRegistrator
    {
        [NotNull]
        IPoeEyeModulesRegistrator RegisterSettingsEditor<TConfig, TSettingsViewModel>()
            where TConfig : class, IPoeEyeConfig, new()
            where TSettingsViewModel : ISettingsViewModel<TConfig>;
    }

    public interface IPoeEyeModulesEnumerator
    {
        ReadOnlyObservableCollection<ISettingsViewModel> Settings { [NotNull] get; }
    }
}