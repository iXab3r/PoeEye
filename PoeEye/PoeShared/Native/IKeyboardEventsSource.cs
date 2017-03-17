using System;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public interface IKeyboardEventsSource
    {
        IObservable<KeyPressEventArgs> WhenKeyPress { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyDown { [NotNull] get; }

        IObservable<KeyEventArgs> WhenKeyUp { [NotNull] get; }
    }
}