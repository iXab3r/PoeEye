using JetBrains.Annotations;
using PoeShared.Scaffolding;
using RadialMenu.Controls;
using ReactiveUI;

namespace PoeChatWheel.ViewModels
{
    public interface IPoeChatWheelViewModel : IDisposableReactiveObject
    {
        bool IsOpen { get; }

        IReactiveList<RadialMenuItem> Items { [NotNull] get; }

        RadialMenuCentralItem CentralItem { [CanBeNull] get; }
    }
}