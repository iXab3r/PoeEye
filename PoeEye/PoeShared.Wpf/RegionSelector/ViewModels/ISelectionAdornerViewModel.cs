using System;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using WinSize = System.Drawing.Size;

namespace PoeShared.RegionSelector.ViewModels
{
    public interface ISelectionAdornerViewModel : IDisposableReactiveObject
    {
        double StrokeThickness { get; }
        
        Brush Stroke { [CanBeNull] get; }
        
        Point AnchorPoint { get; }
        
        Size RenderSize { get; }
        
        WinSize ScreenRenderSize { get; }
        
        Point MousePosition { get; }
        
        bool StopWhenFocusLost { get; set; }
        
        bool ShowCrosshair { get; set; }
        
        bool ShowBackground { get; set; }
        
        double BackgroundOpacity { get; set; }
        
        UIElement Owner { [CanBeNull] get; }
        
        [NotNull]
        IObservable<Rect> StartSelection();
    }
}