using System.Collections.ObjectModel;
using System.Windows.Input;
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

        KeyGesture Hotkey { [CanBeNull] get; [CanBeNull] set; }
    }
}