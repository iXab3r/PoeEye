using System;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
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

        float Opacity { get; set; }

        Size MinSize { get; set; }

        Size MaxSize { get; set; }

        bool IsLocked { get; }

        bool IsUnlockable { get; }

        OverlayMode OverlayMode { get; set; }

        IObservable<EventPattern<RoutedEventArgs>> WhenLoaded { [NotNull] get; }

        SizeToContent SizeToContent { get; }

        string Title { [CanBeNull] get; }

        ICommand LockWindowCommand { [NotNull] get; }

        ICommand UnlockWindowCommand { [NotNull] get; }

        void ResetToDefault();

        void SetActivationController([NotNull] IActivationController controller);
    }
}