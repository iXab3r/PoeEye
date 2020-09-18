using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    public interface IOverlayViewModel : IDisposableReactiveObject
    {
        Rect Bounds { get; }
        
        System.Drawing.Rectangle NativeBounds { get; }
        
        PointF Dpi { get; }
    
        double Left { get; set; }

        double Top { get; set; }

        double Width { get; set; }

        double Height { get; set; }

        double ActualWidth { get; set; }

        double ActualHeight { get; set; }

        float Opacity { get; set; }

        Size MinSize { get; set; }

        Size MaxSize { get; set; }

        bool ShowInTaskbar { get; set; }

        bool EnableHeader { get; set; }
        
        bool IsLocked { get; }

        bool IsUnlockable { get; }

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

        /// <summary>
        ///  Invokes action on UI Dispatcher
        ///  FIXME It would be much better to automatically reschedule invocations if needed
        /// </summary>
        /// <param name="dispatcherAction"></param>
        DispatcherOperation BeginInvoke(Action dispatcherAction);
    }
}