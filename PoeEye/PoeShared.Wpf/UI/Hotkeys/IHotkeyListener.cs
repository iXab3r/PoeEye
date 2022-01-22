using PoeShared.Scaffolding;

namespace PoeShared.UI;

public interface IHotkeyListener : IDisposableReactiveObject
{
    bool Activated { get; }
}