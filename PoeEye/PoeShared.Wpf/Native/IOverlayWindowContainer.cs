using System.Windows;

namespace PoeShared.Native;

public interface IOverlayWindowContainer 
{
    DataTemplate ContentTemplate { get; set; }
    bool IsFocusable { get; }
    double MinWidth { get; }
    double MaxWidth { get; }
    double MinHeight { get; }
    double MaxHeight { get; }
    double ResizeThumbSize { get; }
    bool ShowWireframes { get; set; }
    double GlassFrameThickness { get; }
    double ResizeBorderThickness { get; }
    WinRect NativeBounds { get; set; }
}

public interface IOverlayWindowContainer<T> : IOverlayWindowContainer  where T : IOverlayViewModel
{
    new T Content { get; set; }
}