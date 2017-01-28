using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using RadialMenu.Controls;
using ReactiveUI;

namespace PoeChatWheel.ViewModels
{
    public interface IPoeChatWheelViewModel : IOverlayViewModel
    {
        bool IsOpen { get; }

        IReactiveList<RadialMenuItem> Items { [NotNull] get; }

        RadialMenuCentralItem CentralItem { [CanBeNull] get; }
    }
}