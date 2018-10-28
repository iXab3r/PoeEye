using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using CefSharp;
using Common.Logging;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(KeyboardEventsSource));

        private readonly IScheduler kbdScheduler;
        private readonly IKeyboardMouseEvents keyboardMouseEvents;
        private readonly ISubject<KeyEventArgs> whenKeyDown = new Subject<KeyEventArgs>();

        private readonly ISubject<KeyPressEventArgs> whenKeyPress = new Subject<KeyPressEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyUp = new Subject<KeyEventArgs>();
        private readonly ISubject<MouseEventExtArgs> whenMouseDown = new Subject<MouseEventExtArgs>();
        private readonly ISubject<MouseEventExtArgs> whenMouseUp = new Subject<MouseEventExtArgs>();

        public KeyboardEventsSource(
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] ISchedulerProvider schedulerProvider)
        {
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            this.keyboardMouseEvents = keyboardMouseEvents;

            kbdScheduler = schedulerProvider.GetOrCreate("KdbInput");
            InitializeHook();
        }

        public IObservable<KeyPressEventArgs> WhenKeyPress => whenKeyPress;

        public IObservable<KeyEventArgs> WhenKeyDown => whenKeyDown;

        public IObservable<KeyEventArgs> WhenKeyUp => whenKeyUp;
        public IObservable<MouseEventArgs> WhenMouseDown => whenMouseDown.OfType<MouseEventArgs>();
        public IObservable<MouseEventArgs> WhenMouseUp => whenMouseUp.OfType<MouseEventArgs>();

        private void InitializeHook()
        {
            Log.Debug($"[KeyboardEventsSource] Hook: {keyboardMouseEvents}");
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
            
            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => keyboardMouseEvents.MouseDownExt += h,
                    h => keyboardMouseEvents.MouseDownExt -= h)
                .Select(x => x.EventArgs)
                .ObserveOn(kbdScheduler)
                .Do(whenMouseDown)
                .Where(x => x.Handled)
                .Subscribe(LogEvent, Log.HandleException)
                .AddTo(Anchors);
            
            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => keyboardMouseEvents.MouseUpExt += h,
                    h => keyboardMouseEvents.MouseUpExt -= h)
                .Select(x => x.EventArgs)
                .ObserveOn(kbdScheduler)
                .Do(whenMouseUp)
                .Where(x => x.Handled)
                .Subscribe(LogEvent, Log.HandleException)
                .AddTo(Anchors);
        }

        private void LogEvent(object arg)
        {
            if (!Log.IsTraceEnabled)
            {
                return;
            }

            Log.Trace($"[KeyboardEventsSource] {arg.DumpToText(Formatting.None)}");
        }
    }
}