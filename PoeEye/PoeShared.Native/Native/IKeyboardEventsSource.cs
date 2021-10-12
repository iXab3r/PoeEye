using System;
using System.Windows.Forms;
using JetBrains.Annotations;
using WindowsHook;

namespace PoeShared.Native
{
    public interface IKeyboardEventsSource : ISupportsKeyboardFilter, ISupportsMouseFilter
    {
        bool RealtimeMode { get; }

        IObservable<KeyEventArgsExt> WhenKeyRaw { [NotNull] get; }
        
        IObservable<MouseEventExtArgs> WhenMouseRaw { [NotNull] get; }
        
        IObservable<KeyPressEventArgs> WhenKeyPress { [NotNull] get; }

        IObservable<KeyEventArgsExt> WhenKeyDown { [NotNull] get; }

        IObservable<KeyEventArgsExt> WhenKeyUp { [NotNull] get; }

        IObservable<MouseEventExtArgs> WhenMouseUp { [NotNull] get; }

        IObservable<MouseEventExtArgs> WhenMouseDown { [NotNull] get; }
        
        IObservable<MouseEventExtArgs> WhenMouseMove { [NotNull] get; }
        
        IObservable<MouseEventExtArgs> WhenMouseWheel { [NotNull] get; }
    }
}