using System.Collections.ObjectModel;
using PoeShared.PoeTrade;
using ReactiveUI;

namespace PoeEye.PoeTrade.Models
{
    public interface IPoeApiProvider : IReactiveObject
    {
        ReadOnlyObservableCollection<IPoeApiWrapper> ModulesList { get; }
    }
}