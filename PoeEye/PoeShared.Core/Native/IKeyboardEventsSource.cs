using System;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IKeyboardEventsSource
    {
        bool RealtimeMode { get; set; }

        IDisposable InitializeMouseHook();
        
        IDisposable InitializeKeyboardHook();
        
        IObservable<KeyPressEventArgs> WhenKeyPress { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyDown { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyUp { [NotNull] get; }

        IObservable<MouseEventArgs> WhenMouseUp { [NotNull] get; }

        IObservable<MouseEventArgs> WhenMouseDown { [NotNull] get; }
        
        IObservable<MouseEventArgs> WhenMouseMove { [NotNull] get; }
    }
}