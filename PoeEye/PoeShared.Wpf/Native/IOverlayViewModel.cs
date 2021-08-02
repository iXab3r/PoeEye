using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public interface IOverlayViewModel : IDisposableReactiveObject
    {
        System.Drawing.Rectangle NativeBounds { get; set; }
        
        PointF Dpi { get; }

        double ActualWidth { get; }

        double ActualHeight { get; }

        float Opacity { get; set; }

        Size MinSize { get; set; }

        Size MaxSize { get; set; }

        bool ShowInTaskbar { get; set; }

        bool EnableHeader { get; set; }
        
        bool IsLocked { get; }

        bool IsUnlockable { get; }
        
        bool IsVisible { get; set; }

        OverlayMode OverlayMode { get; set; }
        
        SizeToContent SizeToContent { get; }

        string Title { [CanBeNull] get; }

        ICommand LockWindowCommand { [NotNull] get; }

        ICommand UnlockWindowCommand { [NotNull] get; }
        
        ICommand MakeLayeredCommand { [NotNull] get; }

        ICommand MakeTransparentCommand { [NotNull] get; }
        
        double? TargetAspectRatio { get; set; }
        
        Point ViewModelLocation { get; }
        
        TransparentWindow OverlayWindow { [CanBeNull] get; }

        void ResetToDefault();

        void SetActivationController([NotNull] IActivationController controller);

        void SetOverlayWindow([NotNull] TransparentWindow owner);
    }
}