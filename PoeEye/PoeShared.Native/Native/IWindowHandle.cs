using System;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using PInvoke;

namespace PoeShared.Native
{
    public interface IWindowHandle : IWin32Window, IDisposable, IEquatable<IWindowHandle>
    {
        string Title { [CanBeNull] get; }
        
        Rectangle WindowBounds { get; }
        
        Rectangle ClientBounds { get; }
        
        Rectangle DwmWindowBounds { get; }
        
        Rectangle ClientRectWithinMonitor { get; }
        
        Icon Icon { get; }
        
        BitmapSource IconBitmap { get; }
        
        string Class { get; }
        
        int ProcessId { get; }
        
        DateTime CreatedAt { get; }
        
        string ProcessPath { [CanBeNull] get; }
        
        string ProcessName { [CanBeNull] get; }
        
        string ProcessArgs { [CanBeNull] get; }

        string CommandLine { [CanBeNull] get; }
        
        int ZOrder { get; set; }
        
        User32.WindowStyles WindowStyle { get; }
        
        User32.WindowStylesEx WindowStylesEx { get; }
        
        bool IsVisible { get; }
        
        bool IsIconic { get; }
    }
}