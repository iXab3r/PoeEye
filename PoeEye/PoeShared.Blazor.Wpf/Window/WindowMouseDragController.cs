using System;
using System.Reactive.Disposables;
using System.Windows.Input;
using PoeShared.Scaffolding;
using System.Drawing;

namespace PoeShared.Blazor.Wpf;

internal abstract class WindowMouseDragControllerBase : DisposableReactiveObject
{
    public WindowMouseDragControllerBase(IBlazorWindow blazorWindow, BlazorContentControl contentControl)
    {
        BlazorWindow = blazorWindow;
        ContentControl = contentControl;
            
        StartPoint = System.Windows.Forms.Cursor.Position;
        WindowInitialPosition = new Point(blazorWindow.Left, blazorWindow.Top);
        
        if (!ContentControl.CaptureMouse())
        {
            throw new ApplicationException($"Failed to capture mouse inside window {blazorWindow}");
        }
            
        ContentControl.MouseUp += ControlOnMouseUp;
        ContentControl.MouseMove += ControlOnMouseMove;
        
        Disposable.Create(() =>
        {
            ContentControl.MouseUp -= ControlOnMouseUp;
            ContentControl.MouseMove -= ControlOnMouseMove;
            try
            {
                ContentControl.ReleaseMouseCapture();
            }
            catch (Exception)
            {
                //not critical at this point
            }
        }).AddTo(Anchors);
    }
        
    public IBlazorWindow BlazorWindow { get; }
        
    public BlazorContentControl ContentControl { get; }
    
    public Point WindowInitialPosition { get; }

    public Point StartPoint { get; }

    public bool IsDragging => !Anchors.IsDisposed;

    protected abstract void HandleMove(Point cursorPosition);

    private void ControlOnMouseMove(object sender, MouseEventArgs e)
    {
        var current = System.Windows.Forms.Cursor.Position;
        HandleMove(current);
    }
        
    private void ControlOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        Dispose();
    }
}

internal sealed class WindowMouseDragController : WindowMouseDragControllerBase
{
    public WindowMouseDragController(IBlazorWindow blazorWindow, BlazorContentControl contentControl) : base(blazorWindow, contentControl)
    {
        
    }

    protected override void HandleMove(Point cursorPosition)
    {
        var diffX = cursorPosition.X - StartPoint.X;
        var diffY = cursorPosition.Y - StartPoint.Y;
        BlazorWindow.SetWindowPos(new Point(WindowInitialPosition.X + diffX, WindowInitialPosition.Y + diffY));
    }
}