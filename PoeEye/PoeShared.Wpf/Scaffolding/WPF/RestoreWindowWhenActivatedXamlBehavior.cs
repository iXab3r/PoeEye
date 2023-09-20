using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using PoeShared.Logging;

namespace PoeShared.Scaffolding.WPF;

/// <summary>
/// Manually manages WindowState when window is Activated.
/// This helps to fix a problem when minimized window does not correctly restores using Alt+Tab/Taskbar
/// Dirty hack for EA-218 Window-does-not-get-restored-after-minimization
/// </summary>
public sealed class RestoreWindowWhenActivatedXamlBehavior : Behavior<Window>
{
    private static readonly IFluentLog Log = typeof(RestoreWindowWhenActivatedXamlBehavior).PrepareLogger();

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Activated += AssociatedObjectOnActivated;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Activated -= AssociatedObjectOnActivated;
    }

    private static void AssociatedObjectOnActivated(object sender, EventArgs e)
    {
        var window = sender as Window;
        if (window == null)
        {
            return;
        }

        if (window.WindowState != WindowState.Minimized)
        {
            return;
        }

        Log.Debug($"Window is activated, but minimized, restoring window state to Normal for {window}");
        window.WindowState = WindowState.Normal;
    }
}