using JetBrains.Annotations;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IGenericSettingsViewModel : IDisposableReactiveObject, IMessageBoxViewModel, ICloseable
{
    IReadOnlyObservableCollection<ISettingsViewModel> ModulesSettings { [NotNull] get; }

    void SaveConfigs();
    
    void ReloadConfigs();
}