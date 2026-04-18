using PoeShared.Scaffolding;

namespace PoeShared.UI.Avalonia;

public sealed class MainCounterViewModel : DisposableReactiveObject
{
    public MainCounterViewModel(string? displayName = null)
    {
        DisplayName = displayName ?? "Counter content";
    }

    public Guid InstanceId { get; } = Guid.NewGuid();

    public string DisplayName { get; set; }

    public int Count { get; set; }
}
