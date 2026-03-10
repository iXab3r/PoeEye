using System;
using PoeShared.Scaffolding;

namespace PoeShared.UI.WinForms.Blazor;

public class MainCounterViewModel : DisposableReactiveObject
{
    private int count;
    private string displayName;

    public MainCounterViewModel(string? displayName = null)
    {
        this.displayName = displayName ?? "Counter content";
    }

    public Guid InstanceId { get; } = Guid.NewGuid();

    public string DisplayName
    {
        get => displayName;
        set => RaiseAndSetIfChanged(ref displayName, value);
    }

    public int Count
    {
        get => count;
        set => RaiseAndSetIfChanged(ref count, value);
    }
}
