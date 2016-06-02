using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    public interface IPoeApiSelectorViewModel : IDisposableReactiveObject
    {
        IPoeApiWrapper SelectedModule { get; set; }

        void SetByModuleName([NotNull] string moduleName);
    }
}