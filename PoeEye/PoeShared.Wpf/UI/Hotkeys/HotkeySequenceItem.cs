using System.Reactive.Linq;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Hotkeys
{
    public abstract class HotkeySequenceItem : DisposableReactiveObject
    {
        public virtual bool IsVisible { get; } = true;

        public virtual bool IsDragDropSource { get; } = true;
    }
}