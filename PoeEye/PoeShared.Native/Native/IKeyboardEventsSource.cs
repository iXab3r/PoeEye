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

        IObservable<KeyEventArgs> WhenKeyDown { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyUp { [NotNull] get; }

        IObservable<MouseEventArgs> WhenMouseUp { [NotNull] get; }

        IObservable<MouseEventArgs> WhenMouseDown { [NotNull] get; }
        
        IObservable<MouseEventArgs> WhenMouseMove { [NotNull] get; }
        
        IObservable<MouseEventArgs> WhenMouseWheel { [NotNull] get; }
    }
}