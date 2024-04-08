using System;
using System.Drawing;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Size = System.Drawing.Size;

namespace PoeShared.Native;

public interface IWindowViewModel : IDisposableReactiveObject, ICanBeActive, ICanBeVisible
{
    System.Drawing.Rectangle NativeBounds { get; set; }

    Size MinSize { get; set; }
    
    Size DefaultSize { get; set; }

    Size MaxSize { get; set; }

    PointF Dpi { get; }

    SizeToContent SizeToContent { get; }

    string Id { [NotNull] get; }

    string Title { [CanBeNull] get; }
    
    IWindowViewController WindowController { [CanBeNull] get; }

    IObservable<KeyEventArgs> WhenKeyUp { get; }
    IObservable<KeyEventArgs> WhenKeyDown { get; }
    IObservable<KeyEventArgs> WhenPreviewKeyDown { get; }
    IObservable<KeyEventArgs> WhenPreviewKeyUp { get; }

    bool ShowInTaskbar { get; set; }

    bool EnableHeader { get; set; }
    
    bool IsLoaded { get; }
    
    double? TargetAspectRatio { get; set; }
    
    public void SetOverlayWindow([NotNull] IWindowViewController owner);
}