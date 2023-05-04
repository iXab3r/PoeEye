using System;
using System.Drawing;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using Size = System.Drawing.Size;

namespace PoeShared.Native;

public interface IWindowViewModel : IDisposableReactiveObject
{
    System.Drawing.Rectangle NativeBounds { get; set; }

    Size MinSize { get; set; }

    Size MaxSize { get; set; }

    PointF Dpi { get; }

    SizeToContent SizeToContent { get; }

    string Id { [NotNull] get; }

    string Title { [CanBeNull] get; }

    TransparentWindow OverlayWindow { [CanBeNull] get; }

    IObservable<EventPattern<KeyEventArgs>> WhenKeyUp { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenKeyDown { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenPreviewKeyDown { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenPreviewKeyUp { get; }

    void SetOverlayWindow([NotNull] TransparentWindow owner);

    bool IsVisible { get; set; }

    bool ShowInTaskbar { get; set; }

    bool EnableHeader { get; set; }
    
    bool IsLoaded { get; }
    
    bool IsActive { get; }
    
    double? TargetAspectRatio { get; set; }
}