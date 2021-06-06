using System.Collections.ObjectModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.UI
{
    public interface IGenericSettingsViewModel : IDisposableReactiveObject
    {
        ReadOnlyObservableCollection<ISettingsViewModel> ModulesSettings { [NotNull] get; }
        bool IsOpen { get; set; }
        ICommand SaveConfigCommand { [NotNull] get; }
        ICommand CancelCommand { [NotNull] get; }
    }
}