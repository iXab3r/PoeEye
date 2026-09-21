using System;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Explicit host default for application-level dialogs. A desktop host registers its root HWND;
/// independent script/SDK hosts may omit this service. Never resolves the current foreground window.
/// A dialog operation's explicit owner (including zero for no owner) takes precedence.
/// </summary>
public interface IApplicationWindowOwner
{
    /// <summary>The host's root HWND, captured once the root native window exists.</summary>
    IntPtr Handle { get; }
}

/// <summary>Host-published root-window identity, readable without entering a WPF dispatcher.</summary>
public sealed class ApplicationWindowOwner : IApplicationWindowOwner
{
    private IntPtr handle;
    public IntPtr Handle => System.Threading.Volatile.Read(ref handle);

    /// <summary>Publishes the root HWND after native creation; zero clears it during host shutdown.</summary>
    public void Publish(IntPtr value) => System.Threading.Volatile.Write(ref handle, value);
}
