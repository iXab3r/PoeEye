using PoeShared.PoeTrade;
using ReactiveUI;

namespace PoeEye.PoeTrade.Models
{
    public interface IPoeApiProvider : IReactiveObject
    {
        IReactiveList<IPoeApiWrapper> ModulesList { get; }
    }
}