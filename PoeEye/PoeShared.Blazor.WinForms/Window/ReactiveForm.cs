using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.WinForms;

internal delegate IntPtr FormMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

internal class ReactiveForm : Form, IDisposableReactiveObject
{
    private bool showActivated = true;
    private WindowState windowState;

    public CompositeDisposable Anchors { get; } = new();

    public event PropertyChangedEventHandler PropertyChanged;

    public event FormMessageHook MessageHook;

    public event EventHandler SourceInitialized;
    public event EventHandler Loaded;
    public event EventHandler Unloaded;
    public event CancelEventHandler Closing;
    public event EventHandler Closed;
    public event EventHandler Activated;
    public event EventHandler Deactivated;
    public event EventHandler StateChanged;

    public IntPtr WindowHandle => IsHandleCreated ? Handle : IntPtr.Zero;

    public bool IsDisposed => Anchors.IsDisposed;

    public bool IsVisible => Visible;

    public bool IsLoaded { get; private set; }

    public Dispatcher Dispatcher { get; } = Dispatcher.CurrentDispatcher;

    public WindowState WindowState
    {
        get => windowState;
        set
        {
            var translated = value switch
            {
                WindowState.Maximized => FormWindowState.Maximized,
                WindowState.Minimized => FormWindowState.Minimized,
                _ => FormWindowState.Normal
            };
            base.WindowState = translated;
        }
    }

    public WindowStartupLocation WindowStartupLocation { get; set; }

    public ResizeMode ResizeMode { get; set; } = ResizeMode.CanResizeWithGrip;

    public bool ShowActivated
    {
        get => showActivated;
        set
        {
            if (showActivated == value)
            {
                return;
            }

            showActivated = value;
            RaisePropertyChanged(nameof(ShowActivated));
        }
    }

    public string Title
    {
        get => Text;
        set => Text = value;
    }

    public bool Topmost
    {
        get => TopMost;
        set => TopMost = value;
    }

    protected override bool ShowWithoutActivation => !ShowActivated;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Anchors.Dispose();
            GC.SuppressFinalize(this);
        }

        base.Dispose(disposing);
    }

    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetActivation(bool isFocusable)
    {
        if (WindowHandle == IntPtr.Zero)
        {
            return;
        }

        if (isFocusable)
        {
            UnsafeNative.SetWindowExActivate(WindowHandle);
        }
        else
        {
            UnsafeNative.SetWindowExNoActivate(WindowHandle);
        }
    }

    public void SetOverlayMode(OverlayMode mode)
    {
        if (WindowHandle == IntPtr.Zero)
        {
            return;
        }

        switch (mode)
        {
            case OverlayMode.Normal:
                UnsafeNative.SetWindowExNormal(WindowHandle);
                break;
            case OverlayMode.Layered:
                UnsafeNative.SetWindowExLayered(WindowHandle);
                break;
            case OverlayMode.Transparent:
                UnsafeNative.SetWindowExTransparent(WindowHandle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        IsLoaded = true;
        RaisePropertyChanged(nameof(IsLoaded));
        Loaded?.Invoke(this, e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // WPF raises SourceInitialized only once the native window source is usable.
        // WinForms HandleCreated can still be too early for code that immediately reads Handle/IsHandleCreated.
        BeginInvoke(new Action(() =>
        {
            if (!IsDisposed && IsHandleCreated)
            {
                SourceInitialized?.Invoke(this, EventArgs.Empty);
            }
        }));
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            Unloaded?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Closing?.Invoke(this, e);
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        Closed?.Invoke(this, e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        Activated?.Invoke(this, e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Deactivated?.Invoke(this, e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var nextState = base.WindowState switch
        {
            FormWindowState.Maximized => WindowState.Maximized,
            FormWindowState.Minimized => WindowState.Minimized,
            _ => WindowState.Normal
        };
        if (nextState != windowState)
        {
            windowState = nextState;
            StateChanged?.Invoke(this, EventArgs.Empty);
            RaisePropertyChanged(nameof(WindowState));
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (MessageHook != null)
        {
            var handled = false;
            var result = MessageHook(WindowHandle, m.Msg, m.WParam, m.LParam, ref handled);
            if (handled)
            {
                m.Result = result;
                return;
            }
        }

        base.WndProc(ref m);
    }
}
