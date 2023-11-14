using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using PInvoke;

namespace PoeShared.Native;

internal readonly struct WindowHandleSnapshot : IWindowHandle
{
    public static readonly WindowHandleSnapshot Empty = new(); 
    
    public IntPtr Handle { get; }
    
    public void Dispose()
    {
    }

    public bool Equals(IWindowHandle other)
    {
        return other?.Handle == this.Handle;
    }

    public string Title { get; }
    public Rectangle WindowBounds { get; }
    public Rectangle ClientBounds { get; }
    public Rectangle DwmWindowBounds { get; }
    public Rectangle DwmWindowBoundsWithinMonitor { get; }
    public Size BorderSize { get; }
    public Icon Icon { get; }
    public BitmapSource IconBitmap { get; }
    public string Class { get; }
    public int ProcessId { get; }
    public int ThreadId { get; }
    public int ParentProcessId { get; }
    public DateTime CreatedAt { get; }
    public string ProcessPath { get; }
    public string ProcessName { get; }
    public string ProcessArgs { get; }
    public string CommandLine { get; }

    public int ZOrder
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    public User32.WindowStyles WindowStyle { get; }
    public User32.WindowStylesEx WindowStylesEx { get; }
    public bool IsVisible { get; }
    public bool IsIconic { get; }
    public IWindowHandle Owner { get; }
    public IWindowHandle Parent { get; }
}