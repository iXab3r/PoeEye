using System;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using WinSize = System.Drawing.Size;
using WinPoint = System.Drawing.Point;
using WinRectangle = System.Drawing.Rectangle;
using WpfRect = System.Windows.Rect;
using WinRect = System.Drawing.Rectangle;
using WpfSize = System.Windows.Size;

namespace PoeShared.RegionSelector.ViewModels
{
    public interface ISelectionAdornerViewModel : IDisposableReactiveObject
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
        
        bool StopWhenFocusLost { get; set; }
        
        bool ShowCrosshair { get; set; }
        
        bool ShowBackground { get; set; }
        
        bool IsVisible { get; }
        
        double BackgroundOpacity { get; set; }
        
        UIElement Owner { [CanBeNull] get; }
        
        IObservable<Rect> StartSelection();
    }
}