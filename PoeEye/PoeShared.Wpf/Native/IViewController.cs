using System;
using System.Reactive;
using System.Runtime.CompilerServices;

namespace PoeShared.Native
{
    public interface IViewController
    {
        IObservable<Unit> WhenLoaded { get; }

        void Hide();

        void Show();
    }
}