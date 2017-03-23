using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private readonly IKeyboardMouseEvents keyboardMouseEvents;
        private readonly IScheduler kbdScheduler;

        private readonly ISubject<KeyPressEventArgs> whenKeyPress = new Subject<KeyPressEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyDown = new Subject<KeyEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyUp = new Subject<KeyEventArgs>();

        public KeyboardEventsSource(
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] ISchedulerProvider schedulerProvider)
        {
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            this.keyboardMouseEvents = keyboardMouseEvents;

            kbdScheduler = schedulerProvider.GetOrCreate("KdbInput");
            InitializeHook();
        }

        public IObservable<KeyPressEventArgs> WhenKeyPress => whenKeyPress;

        public IObservable<KeyEventArgs> WhenKeyDown => whenKeyDown;

        public IObservable<KeyEventArgs> WhenKeyUp => whenKeyUp;

        private void InitializeHook()
        {
            Log.Instance.Debug($"[KeyboardEventsSource] Hook: {keyboardMouseEvents}");
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .ObserveOn(kbdScheduler)
                .Do(whenKeyDown)
                .Where(x => x.Handled || x.SuppressKeyPress)
                .Subscribe(LogEvent, Log.HandleException)
                .AddTo(Anchors);

            Observable
               .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                   h => keyboardMouseEvents.KeyUp += h,
                   h => keyboardMouseEvents.KeyUp -= h)
               .Select(x => x.EventArgs)
               .ObserveOn(kbdScheduler)
               .Do(whenKeyUp)
               .Where(x => x.Handled)
               .Subscribe(LogEvent, Log.HandleException)
               .AddTo(Anchors);

            Observable
               .FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(
                   h => keyboardMouseEvents.KeyPress += h,
                   h => keyboardMouseEvents.KeyPress -= h)
               .Select(x => x.EventArgs)
               .ObserveOn(kbdScheduler)
               .Do(whenKeyPress)
               .Where(x => x.Handled)
               .Subscribe(LogEvent, Log.HandleException)
               .AddTo(Anchors);
        }

        private void LogEvent(object arg)
        {
            if (!Log.Instance.IsDebugEnabled)
            {
                return;
            }
            Log.Instance.Debug($"[KeyboardEventsSource] {arg.DumpToText(Formatting.None)}");
        }
    }
}