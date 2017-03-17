using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using System.Windows.Input;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private static readonly Action NoOperation = () => { };

        private readonly ISubject<KeyPressEventArgs> whenKeyPress = new Subject<KeyPressEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyDown = new Subject<KeyEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyUp = new Subject<KeyEventArgs>();

        public KeyboardEventsSource([NotNull] IKeyboardMouseEvents keyboardMouseEvents)
        {
            Guard.ArgumentNotNull(() => keyboardMouseEvents);

            Log.Instance.Debug($"[KeyboardEventsSource] Hook: {keyboardMouseEvents}");

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Subscribe(whenKeyDown)
                .AddTo(Anchors);

            Observable
               .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                   h => keyboardMouseEvents.KeyUp += h,
                   h => keyboardMouseEvents.KeyUp -= h)
               .Select(x => x.EventArgs)
               .Subscribe(whenKeyUp)
               .AddTo(Anchors);

            Observable
               .FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(
                   h => keyboardMouseEvents.KeyPress += h,
                   h => keyboardMouseEvents.KeyPress -= h)
               .Select(x => x.EventArgs)
               .Subscribe(whenKeyPress)
               .AddTo(Anchors);
        }

        public IObservable<KeyPressEventArgs> WhenKeyPress => whenKeyPress;

        public IObservable<KeyEventArgs> WhenKeyDown => whenKeyDown;

        public IObservable<KeyEventArgs> WhenKeyUp => whenKeyUp;
    }
}