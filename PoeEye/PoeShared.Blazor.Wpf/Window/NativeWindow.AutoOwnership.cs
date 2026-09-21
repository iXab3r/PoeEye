using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;

namespace PoeShared.Blazor.Wpf;

partial class NativeWindow
{
    private static long nextModalShow;
    private IntPtr registryHandle;
    private long modalShow;
    private bool modalPresentationPending;
    private bool modalPresentationCancelled;
    private bool automaticOwnerResolved;
    private WeakReference<NativeWindow> automaticOwner;
    private bool hasBeenPresented;
    // Accessed only on this owner's dispatcher, including requests from dialogs on other dispatchers.
    private int modalDisableCount;
    private bool modalOwnerWasDisabled;

    internal long RegistryId => windowId;
    internal IntPtr RegistryHandle => Volatile.Read(ref registryHandle);
    internal bool RegistryClosed => isClosedTcs.Task.IsCompleted;

    private IntPtr ResolveAutomaticOwner()
    {
        if (!AutoOwner) return IntPtr.Zero;
        if (!automaticOwnerResolved)
        {
            automaticOwnerResolved = true;
            var windows = NativeWindowRegistry.Instance.GetWindows();
            var foreground = User32.GetForegroundWindow();
            var selected = windows.FirstOrDefault(x => x.RegistryHandle == foreground && IsOwnerCandidate(x));
            selected ??= NativeWindowRegistry.Instance.GetMainWindow();
            if (!IsOwnerCandidate(selected)) return IntPtr.Zero;

            var visited = new HashSet<long> { RegistryId };
            while (visited.Add(selected.RegistryId)
                   && (User32.GetWindowLong(selected.RegistryHandle, User32.WindowLongIndexFlags.GWL_STYLE)
                       & (int) User32.WindowStyles.WS_DISABLED) != 0)
            {
                var blocker = windows
                    .Where(x => IsOwnerCandidate(x) && !visited.Contains(x.RegistryId)
                        && Volatile.Read(ref x.modalShow) != 0
                        && Volatile.Read(ref x.appliedOwnerHandle) == selected.RegistryHandle)
                    .OrderByDescending(x => Volatile.Read(ref x.modalShow))
                    .FirstOrDefault();
                if (blocker == null) break;
                selected = blocker;
            }
            automaticOwner = new WeakReference<NativeWindow>(selected);
        }

        // The controller identity, not a reusable HWND, owns the cached automatic relationship.
        return automaticOwner != null && automaticOwner.TryGetTarget(out var owner)
            && !owner.RegistryClosed && User32.IsWindow(owner.RegistryHandle)
                ? owner.RegistryHandle : IntPtr.Zero;
    }

    private bool IsOwnerCandidate(NativeWindow candidate)
    {
        if (candidate == null || ReferenceEquals(candidate, this) || candidate.RegistryClosed
            || candidate.RegistryHandle == IntPtr.Zero || !User32.IsWindowVisible(candidate.RegistryHandle)) return false;
        var seen = new HashSet<IntPtr>();
        for (var current = candidate.RegistryHandle; current != IntPtr.Zero && seen.Add(current);)
        {
            if (current == RegistryHandle) return false;
            current = User32.GetWindow(current, User32.GetWindowCommands.GW_OWNER);
        }
        return true;
    }

    private void PreparePresentation(WindowView window)
    {
        if (window.IsVisible)
        {
            ReconcileTopmost(window);
            return;
        }
        PrepareOwnership(window);
        if (!hasBeenPresented)
        {
            hasBeenPresented = true;
            ApplyWindowStartupLocation(window, WindowStartupLocation);
        }
    }

    private Task UpdateModalDisableAsync(int delta)
    {
        if (RegistryClosed || uiDispatcher.HasShutdownStarted) return Task.CompletedTask;
        // Never synchronously enter another window's dispatcher. Its modal loop may be waiting on ours.
        return uiDispatcher.InvokeAsync(() => UpdateModalDisable(delta)).Task;
    }

    private void UpdateModalDisable(int delta)
    {
        uiDispatcher.VerifyAccess();
        if (RegistryClosed || !User32.IsWindow(RegistryHandle)) return;
        if (delta > 0)
        {
            if (modalDisableCount++ == 0)
                modalOwnerWasDisabled = UnsafeNative.EnableWindow(RegistryHandle, false);
        }
        else if (--modalDisableCount == 0 && !modalOwnerWasDisabled)
        {
            UnsafeNative.EnableWindow(RegistryHandle, true);
        }
    }

    private async Task CompleteModalShowAsync(WindowView window, ShowDialogCommand command)
    {
        try
        {
            await ShowDialogCore(window, command.CancellationToken);
            command.CompletionSource.TrySetResult(true);
        }
        catch (Exception error)
        {
            command.CompletionSource.TrySetException(error);
        }
    }
}
