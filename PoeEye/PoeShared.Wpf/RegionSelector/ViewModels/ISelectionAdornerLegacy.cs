using System;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 

namespace PoeShared.RegionSelector.ViewModels;

public interface ISelectionAdornerLegacy : IDisposableReactiveObject
{
    Size RenderSize { get; }

    [Obsolete("Moved to projection adapter")]
    WinSize ScreenRenderSize { get; }
        
    /// <summary>
    ///   Projection area that selection will be mapped into
    /// </summary>
    WinRect ProjectionBounds { get; set; }
        
    WinRect ProjectedSelection { get; }
        
    WinPoint ProjectedMousePosition { get; }
        
    Point MousePosition { get; }
        
    Rect Selection { get; }
        
    bool StopWhenAppFocusLost { get; set; }
        
    bool ShowCrosshair { get; set; }
        
    bool ShowBackground { get; set; }
        
    bool IsVisible { get; }
        
    double BackgroundOpacity { get; set; }
        
    UIElement Owner { [CanBeNull] get; }
        
    IObservable<Rect> StartSelection(bool supportBoxSelection = true);
}