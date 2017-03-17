using System;
using System.Reactive;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IOverlayViewModel : IDisposableReactiveObject
    {
        double Left { get; set; }

        double Top { get; set; }

        double Width { get; set; }

        double Height { get; set; }

        double ActualWidth { get; set; }

        double ActualHeight { get; set; }

        Size MinSize { get; set; }

        Size MaxSize { get; set; }

        bool IsLocked { get; set; }

        IObservable<Unit> WhenLoaded { [NotNull] get; }

        SizeToContent SizeToContent { get; }

        IOverlayViewModel SetActivationController([NotNull] IActivationController controller);
    }
}