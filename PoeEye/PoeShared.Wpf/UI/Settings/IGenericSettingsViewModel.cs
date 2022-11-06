using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IGenericSettingsViewModel : IDisposableReactiveObject
{
    IReadOnlyObservableCollection<ISettingsViewModel> ModulesSettings { [NotNull] get; }

    void SaveConfigs();
    
    void ReloadConfigs();
}