using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;

namespace PoeEye.PoeTrade.ViewModels
{
    public interface IPoeApiSelectorViewModel : IDisposableReactiveObject
    {
        IPoeApiWrapper SelectedModule { get; set; }

        void SetByModuleId([NotNull] string moduleName);
    }
}