using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using PInvoke;

namespace PoeShared.Blazor.Wpf;

partial class NativeWindow
{
    // Requested pin belongs to the controller. The WPF property is the effective native policy.
    // Registered windows expose scalar snapshots only: never enter an owner's dispatcher from a child.
    private IntPtr appliedOwnerHandle;
    private volatile bool requestedNativeTopmost;
    private int topmostReconciliationPending;
    private readonly SerialDisposable ownerObservation = new();
    private User32.WinEventProc ownerCallback;

    private void InitializeOwnership(WindowView window)
    {
        var handle = window.WindowHandle;
        var source = HwndSource.FromHwnd(handle);
        Volatile.Write(ref registryHandle, handle);
        source.AddHook(OwnershipHook);
        window.Anchors.Add(ownerObservation);
        window.Anchors.Add(Disposable.Create(() =>
        {
            Volatile.Write(ref registryHandle, IntPtr.Zero);
            source.RemoveHook(OwnershipHook);
        }));
    }

    private IntPtr PrepareOwnership(WindowView window)
    {
        uiDispatcher.VerifyAccess();
        var nextOwner = ResolveConfiguredOwnerHandle(window);
        // Reject a cycle before establishing the Win32 relationship (including indirect cycles).
        var visited = new HashSet<IntPtr> { window.WindowHandle };
        for (var ancestor = nextOwner; ancestor != IntPtr.Zero;)
        {
            if (!visited.Add(ancestor)) throw new InvalidOperationException("Window ownership must be acyclic.");
            ancestor = NativeWindowRegistry.Instance.TryGetWindow(ancestor, out var registered)
                ? Volatile.Read(ref registered.appliedOwnerHandle)
                : User32.GetWindow(ancestor, User32.GetWindowCommands.GW_OWNER);
        }

        // Always apply None too: Hide -> OwnerHandle = 0 -> Show must detach the previous owner.
        new WindowInteropHelper(window).Owner = nextOwner;
        Volatile.Write(ref appliedOwnerHandle, nextOwner);
        ObserveOwner(nextOwner);
        ReconcileTopmost(window, positionAboveOwner: nextOwner != IntPtr.Zero);
        return nextOwner;
    }

    private void ObserveOwner(IntPtr owner)
    {
        ownerObservation.Disposable = null;
        if (owner == IntPtr.Zero) return;

        // The cached app-wide wrapper starts at Application.Loaded and never releases its hooks.
        // NativeWindow also supports standalone SDK hosts; this hook belongs to its dispatcher/lifetime.
        ownerCallback ??= (_, eventType, hwnd, _, _, _, _) =>
        {
            // REORDER can identify the desktop rather than the HWND whose layer changed.
            if (hwnd == Volatile.Read(ref appliedOwnerHandle) || eventType == User32.WindowsEventHookType.EVENT_OBJECT_REORDER)
                ScheduleTopmostReconciliation();
        };
        var hook = User32.SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_REORDER,
            User32.WindowsEventHookType.EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, ownerCallback,
            0, 0, User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);
        if (hook.IsInvalid)
        {
            hook.Dispose();
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to observe window owner");
        }
        ownerObservation.Disposable = hook;
    }

    private bool CalculateEffectiveTopmost(HashSet<IntPtr> visited)
    {
        if (requestedNativeTopmost) return true;
        var owner = Volatile.Read(ref appliedOwnerHandle);
        if (owner == IntPtr.Zero || !visited.Add(owner)) return false;
        return NativeWindowRegistry.Instance.TryGetWindow(owner, out var registered)
            ? registered.CalculateEffectiveTopmost(visited)
            : ReadNativeTopmost(owner);
    }

    private void ReconcileTopmost(WindowView window, bool positionAboveOwner = false)
    {
        uiDispatcher.VerifyAccess();
        var target = CalculateEffectiveTopmost(new HashSet<IntPtr>());
        window.Topmost = target;

        var handle = window.WindowHandle;
        if (handle != IntPtr.Zero && (ReadNativeTopmost(handle) != target || positionAboveOwner || IsBelowOwner(handle)))
        {
            // No SHOWWINDOW: showing belongs to Show/ShowDialog. No activation or geometry changes.
            // HWND_NOTOPMOST is a no-op once the bit is already clear. Reorder within the normal
            // band with HWND_TOP in that case, otherwise a child can remain behind its owner.
            // Allow Win32 to normalize the owned group: NOOWNERZORDER can leave a demoted
            // descendant below its owner even when SetWindowPos succeeds.
            var insertAfter = target ? User32.SpecialWindowHandles.HWND_TOPMOST
                : ReadNativeTopmost(handle) ? User32.SpecialWindowHandles.HWND_NOTOPMOST
                : User32.SpecialWindowHandles.HWND_TOP;
            if (!User32.SetWindowPos(handle,
                    insertAfter,
                    0, 0, 0, 0,
                    User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOSIZE |
                    User32.SetWindowPosFlags.SWP_NOACTIVATE))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to apply window topmost policy");
            }
        }
    }

    private static bool ReadNativeTopmost(IntPtr handle) =>
        (User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE) & 0x00000008) != 0;

    private bool IsBelowOwner(IntPtr handle)
    {
        var owner = Volatile.Read(ref appliedOwnerHandle);
        if (owner == IntPtr.Zero || !User32.IsWindowVisible(handle) || !User32.IsWindowVisible(owner)) return false;
        // A matching WS_EX_TOPMOST alone does not establish owned-window ordering after native demotion.
        var visited = new HashSet<IntPtr>();
        for (var preceding = User32.GetWindow(handle, User32.GetWindowCommands.GW_HWNDPREV);
             preceding != IntPtr.Zero && visited.Add(preceding);
             preceding = User32.GetWindow(preceding, User32.GetWindowCommands.GW_HWNDPREV))
        {
            if (preceding == owner) return true;
        }
        return false;
    }

    private IntPtr OwnershipHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Win32 propagates layer changes through ownership, including changes made by a raw HWND owner.
        // Observe the completed transition on this HWND; leave WPF/default message processing intact.
        if (message == (int) User32.WindowMessage.WM_WINDOWPOSCHANGED)
        {
            ScheduleTopmostReconciliation();
        }
        return IntPtr.Zero;
    }

    private void ScheduleTopmostReconciliation()
    {
        if (isClosedTcs.Task.IsCompleted || Interlocked.Exchange(ref topmostReconciliationPending, 1) != 0) return;
        // EnqueueUpdate executes synchronously on this dispatcher. Always defer native-hook feedback.
        uiDispatcher.BeginInvoke(new Action(() =>
        {
            Interlocked.Exchange(ref topmostReconciliationPending, 0);
            if (!isClosedTcs.Task.IsCompleted && windowSupplier.IsValueCreated)
            {
                EnqueueUpdate(new ReconcileTopmostCommand());
            }
        }));
    }

    private sealed record ReconcileTopmostCommand : IWindowCommand;
}
