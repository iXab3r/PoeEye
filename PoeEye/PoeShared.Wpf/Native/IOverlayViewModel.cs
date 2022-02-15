using System;
using System.Drawing;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using Size = System.Drawing.Size;
using WpfPoint = System.Windows.Point;

namespace PoeShared.Native;

public interface IOverlayViewModel : IDisposableReactiveObject
{
    System.Drawing.Rectangle NativeBounds { get; set; }
        
    PointF Dpi { get; }

    float Opacity { get; set; }

    Size MinSize { get; set; }

    Size MaxSize { get; set; }

    bool ShowInTaskbar { get; set; }

    bool EnableHeader { get; set; }
        
    bool IsLoaded { get; }
        
    bool IsLocked { get; set; }
        
    bool IsUnlockable { get; }
        
    bool IsVisible { get; set; }

    OverlayMode OverlayMode { get; set; }
        
    SizeToContent SizeToContent { get; }
        
    string Id { [NotNull] get; }

    string Title { [CanBeNull] get; }

    ICommand LockWindowCommand { [NotNull] get; }

    ICommand UnlockWindowCommand { [NotNull] get; }
        
    ICommand MakeLayeredCommand { [NotNull] get; }

    ICommand MakeTransparentCommand { [NotNull] get; }
        
    double? TargetAspectRatio { get; set; }
        
    WpfPoint ViewModelLocation { get; }
        
    TransparentWindow OverlayWindow { [CanBeNull] get; }
    
    IObservable<EventPattern<KeyEventArgs>> WhenKeyDown { get; }

    void ResetToDefault();

    void SetActivationController([NotNull] IActivationController controller);

    void SetOverlayWindow([NotNull] TransparentWindow owner);
}