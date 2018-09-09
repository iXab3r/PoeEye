using System;
using System.Windows.Forms;
using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IKeyboardEventsSource
    {
        IObservable<KeyPressEventArgs> WhenKeyPress { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyDown { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyUp { [NotNull] get; }
    }
}