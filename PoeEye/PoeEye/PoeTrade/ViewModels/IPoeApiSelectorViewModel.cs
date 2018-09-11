using System.Collections.ObjectModel;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    public interface IPoeApiSelectorViewModel : IDisposableReactiveObject, IPoeStaticDataSource
    {
        IPoeApiWrapper SelectedModule { [CanBeNull] get; [CanBeNull] set; }
        
        ReadOnlyObservableCollection<IPoeApiWrapper> ModulesList { [NotNull] get; }

        IPoeApiWrapper SetByModuleId([CanBeNull] string moduleName);
    }
}