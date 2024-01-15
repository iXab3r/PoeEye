using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Forms;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.UI;

internal sealed class WindowFinderOverlayForm : ReactiveForm
{
    private static readonly Binder<WindowFinderOverlayForm> Binder = new();
    private static readonly WinRect BoundsOffset = new WinRect(-15, -15, 30, 30);

    private readonly Pen dwmFramePen;
    private readonly Pen clientRectPen;
    private readonly Pen titleRectPen;
    private readonly Pen borderPen;

    static WindowFinderOverlayForm()
    {
    }

    public WindowFinderOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        TopLevel = true;
        StartPosition = FormStartPosition.Manual;
        TransparencyKey = BackColor;
        ShowInTaskbar = false;
        borderPen = new Pen(WinColor.White, 1).AddTo(Anchors);
        dwmFramePen = new Pen(WinColor.Red, 2).AddTo(Anchors);
        clientRectPen = new Pen(WinColor.Green, 2).AddTo(Anchors);
        titleRectPen = new Pen(WinColor.Yellow, 2).AddTo(Anchors);

        this.WhenAnyValue(x => x.WindowHandle)
            .ObserveOn(Scheduler.Default)
            .Select(ToWindowSnapshot)
            .ObserveOnDispatcher()
            .Subscribe(x =>
            {
                Window = x;
                Bounds = x?.OverlayBounds ?? WinRect.Empty;
            })
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
    }

    public IWindowHandle WindowHandle { get; set; }

    public WindowSnapshot? Window { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(BackColor);

        var window = Window ?? default;
        if (window.Handle == IntPtr.Zero)
        {
            return;
        }

        e.Graphics.DrawRectangle(borderPen, 0, 0, Bounds.Width-borderPen.Width, Bounds.Height-borderPen.Width);

        if (!window.DwmFrameBounds.IsEmpty)
        {
            var windowRectLocal = ToLocal(window.DwmFrameBounds, window.TransformationMatrix);
            e.Graphics.DrawRectangle(dwmFramePen, windowRectLocal);
        }

        var clientRect = window.ClientRect;
        if (!clientRect.IsEmpty)
        {
            var local = ToLocal(clientRect, window.TransformationMatrix);
            e.Graphics.DrawRectangle(clientRectPen, local);
        }

        var titleRect = window.TitleRect;
        if (!titleRect.IsEmpty)
        {
            var local = ToLocal(titleRect, window.TransformationMatrix);
            e.Graphics.DrawRectangle(titleRectPen, local);
        }
    }

    private static RectangleF ToLocal(WinRect bounds, Matrix3x2 matrix)
    {
        var topLeft = Vector2.Transform(new Vector2(bounds.Left, bounds.Top), matrix);
        var bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), matrix);
        return RectangleF.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private static WindowSnapshot? ToWindowSnapshot(IWindowHandle window)
    {
        if (window == null || window.Handle == IntPtr.Zero)
        {
            return null;
        }
        
        var windowFrame = window.DwmFrameBounds;
        if (windowFrame.IsEmpty)
        {
            try
            {
                var processId = window.ProcessId;
                var process = Process.GetProcessById(processId);
                var mainWindowHandle = process.MainWindowHandle;
                if (mainWindowHandle != window.Handle)
                {
                    return ToWindowSnapshot(new WindowHandle(mainWindowHandle));
                }
            }
            catch (Exception)
            {
                //ignore as we may not have enough permissions, etc
            }
        }
        var frameBounds = windowFrame.OffsetBy(BoundsOffset);
        return new WindowSnapshot()
        {
            OverlayBounds = frameBounds,
            DwmFrameBounds = windowFrame,
            WindowRect = window.WindowRect,
            ClientRect = window.ClientRect,
            TitleRect = window.TitleBarRect,
            Handle = window.Handle,
            Title = window.Title,
            TransformationMatrix = Matrix3x2.CreateTranslation(-frameBounds.Left, -frameBounds.Top)
        };
    }

    public readonly record struct WindowSnapshot
    {
        public IntPtr Handle { get; init; }
        public string Title { get; init; }
        public Matrix3x2 TransformationMatrix { get; init; }
        public WinRect DwmFrameBounds { get; init; }
        public WinRect ClientRect { get; init; }
        public WinRect WindowRect { get; init; }
        public WinRect TitleRect { get; init; }
        public WinRect OverlayBounds { get; init; }
    }
}