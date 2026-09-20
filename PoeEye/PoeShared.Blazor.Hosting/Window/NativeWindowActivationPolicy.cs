namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Immutable host-wide activation constraint, captured when a native window is constructed.
/// Suppression takes precedence over per-window ShowActivated, NoActivate and Activate requests.
/// Hosts that do not register this policy retain ordinary per-window activation behavior.
/// </summary>
public sealed record NativeWindowActivationPolicy
{
    public bool SuppressActivation { get; init; }
}
