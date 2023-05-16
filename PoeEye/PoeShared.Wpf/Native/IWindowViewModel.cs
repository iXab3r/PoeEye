using System;
using System.Drawing;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Size = System.Drawing.Size;

namespace PoeShared.Native;

public interface IWindowViewModel : IDisposableReactiveObject, ICanBeActive, ICanBeVisible
{
    System.Drawing.Rectangle NativeBounds { get; set; }

    Size MinSize { get; set; }

    Size MaxSize { get; set; }

    PointF Dpi { get; }

    SizeToContent SizeToContent { get; }

    string Id { [NotNull] get; }

    string Title { [CanBeNull] get; }

    ReactiveMetroWindow ParentWindow { [CanBeNull] get; }

    IObservable<EventPattern<KeyEventArgs>> WhenKeyUp { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenKeyDown { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenPreviewKeyDown { get; }
    IObservable<EventPattern<KeyEventArgs>> WhenPreviewKeyUp { get; }

    bool ShowInTaskbar { get; set; }

    bool EnableHeader { get; set; }
    
    bool IsLoaded { get; }
    
    double? TargetAspectRatio { get; set; }
    
    internal void SetOverlayWindow([NotNull] ReactiveMetroWindow owner);
}