using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Input;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor.Wpf;

internal abstract class WindowMouseDragControllerBase : DisposableReactiveObject
{
    public WindowMouseDragControllerBase(IBlazorWindow blazorWindow, BlazorContentControl contentControl)
    {
        BlazorWindow = blazorWindow;
        ContentControl = contentControl;
            
        StartPoint = GetCursorPosition();
        WindowInitialPosition = new Point(blazorWindow.Left, blazorWindow.Top);
        
        blazorWindow.Log.Debug($"Window dragging has been started: {new { StartPoint, WindowInitialPosition }}");
        
        if (!ContentControl.CaptureMouse())
        {
            throw new ApplicationException($"Failed to capture mouse inside window {blazorWindow}");
        }
            
        ContentControl.MouseUp += ControlOnMouseUp;
        ContentControl.MouseMove += ControlOnMouseMove;
        
        Disposable.Create(() =>
        {
            blazorWindow.Log.Debug($"Window dragging has stopped: {new { StartPoint, EndPoint = GetCursorPosition(), WindowInitialPosition, WindowPosition = new Point(blazorWindow.Left, blazorWindow.Top) }}");
            
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
        var current = GetCursorPosition();
        HandleMove(current);
    }
        
    private void ControlOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        Dispose();
    }

    private static Point GetCursorPosition()
    {
        return System.Windows.Forms.Cursor.Position;
    }
}