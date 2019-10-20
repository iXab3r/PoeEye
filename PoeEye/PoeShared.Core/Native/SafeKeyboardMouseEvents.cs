using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(KeyboardEventsSource));

        private readonly IClock clock;

        private readonly BlockingCollection<InputEventData> eventQueue = new BlockingCollection<InputEventData>();
        private readonly SerialDisposable subscription = new SerialDisposable();
        private readonly ISubject<KeyEventArgs> whenKeyDown = new Subject<KeyEventArgs>();

        private readonly ISubject<KeyPressEventArgs> whenKeyPress = new Subject<KeyPressEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyUp = new Subject<KeyEventArgs>();
        private readonly ISubject<MouseEventArgs> whenMouseDown = new Subject<MouseEventArgs>();
        private readonly ISubject<MouseEventArgs> whenMouseUp = new Subject<MouseEventArgs>();

        private bool realtimeMode = false;

        public KeyboardEventsSource([NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Log.Debug("Mouse&keyboard event source initialization started");

            this.clock = clock;

            subscription.AddTo(Anchors);
            InitializeConsumer().AddTo(Anchors);
        }

        public IObservable<KeyPressEventArgs> WhenKeyPress => whenKeyPress;

        public IObservable<KeyEventArgs> WhenKeyDown => whenKeyDown;

        public IObservable<KeyEventArgs> WhenKeyUp => whenKeyUp;

        public IObservable<MouseEventArgs> WhenMouseDown => whenMouseDown.OfType<MouseEventArgs>();

        public IObservable<MouseEventArgs> WhenMouseUp => whenMouseUp.OfType<MouseEventArgs>();

        public bool RealtimeMode
        {
            get => realtimeMode;
            set => this.RaiseAndSetIfChanged(ref realtimeMode, value);
        }

        public IDisposable InitializeHooks()
        {
            Log.Info("Configuring Mouse&Keyboard hooks...");
            var sw = Stopwatch.StartNew();

            var anchors = new CompositeDisposable();
            subscription.Disposable = anchors;

            Disposable.Create(() => Log.Info("Disposing Mouse&Keyboard hooks")).AddTo(anchors);
            var hook = Hook.GlobalEvents().AddTo(anchors);
            InitializeKeyboardHook(hook).AddTo(anchors);
            InitializeMouseHook(hook).AddTo(anchors);

            sw.Stop();
            Log.Info($"Mouse&Keyboard hooks configuration took {sw.ElapsedMilliseconds:F0}ms");
            return anchors;
        }

        private IDisposable InitializeConsumer()
        {
            Log.Debug("Creating new event consumer thread");
            var consumer = new CancellationTokenSource();
            var consumerThread = new Thread(InitializeConsumerThread)
            {
                Name = "Input",
                IsBackground = true
            };
            consumerThread.SetApartmentState(ApartmentState.STA);
            consumerThread.Start(consumer.Token);

            return Disposable.Create(() =>
            {
                Log.Debug("Cancelling consumer thread");
                consumer.Cancel();
                Log.Debug("Sent Cancel to consumer thread");
            });
        }

        private void InitializeConsumerThread(object arg)
        {
            var consumer = (CancellationToken) arg;
            InitializeConsumerThread(consumer);
        }

        private void InitializeConsumerThread(CancellationToken cancellationToken)
        {
            try
            {
                Log.Debug("Input event consumer started");
                while (!cancellationToken.IsCancellationRequested)
                {
                    var nextEvent = eventQueue.Take(cancellationToken);
                    ProcessEvent(nextEvent);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Input event consumer received Cancellation request");
            }
            catch (Exception e)
            {
                Log.HandleException(e);
            }
            finally
            {
                Log.Debug("Input event consumer completed");
            }
        }

        private void ProcessEvent(InputEventData nextEvent)
        {
            switch (nextEvent.EventArgs)
            {
                case KeyEventArgs args:
                    switch (nextEvent.EventType)
                    {
                        case InputEventType.KeyDown:
                            whenKeyDown.OnNext(args);
                            break;
                        case InputEventType.KeyUp:
                            whenKeyUp.OnNext(args);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(nextEvent), nextEvent.EventType,
                                $"Invalid enum value for type {args.GetType().Name}, data: {nextEvent.DumpToTextRaw()}");
                    }
                    break;
                case KeyPressEventArgs args:
                    switch (nextEvent.EventType)
                    {
                        case InputEventType.KeyPress:
                            whenKeyPress.OnNext(args);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(nextEvent), nextEvent.EventType,
                                $"Invalid enum value for type {args.GetType().Name}, data: {nextEvent.DumpToTextRaw()}");
                    }
                    break;
                case MouseEventArgs args:
                    switch (nextEvent.EventType)
                    {
                        case InputEventType.MouseDown:
                            whenMouseDown.OnNext(args);
                            break;
                        case InputEventType.MouseUp:
                            whenMouseUp.OnNext(args);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(nextEvent), nextEvent.EventType,
                                $"Invalid enum value for type {args.GetType().Name}, data: {nextEvent.DumpToTextRaw()}");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(nextEvent), nextEvent.EventType,
                        $"Invalid argument type: {nextEvent.EventArgs.GetType().Name}, data: {nextEvent.DumpToTextRaw()}");
            }
        }

        private IDisposable InitializeKeyboardHook(IKeyboardEvents keyboardEvents)
        {
            var anchors = new CompositeDisposable();
            Log.Debug($"Hook: {keyboardEvents}");
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardEvents.KeyDown += h,
                    h => keyboardEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyDown), Log.HandleException)
                .AddTo(anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardEvents.KeyUp += h,
                    h => keyboardEvents.KeyUp -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyUp), Log.HandleException)
                .AddTo(anchors);

            Observable
                .FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(
                    h => keyboardEvents.KeyPress += h,
                    h => keyboardEvents.KeyPress -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyPress), Log.HandleException)
                .AddTo(anchors);

            return anchors;
        }

        private IDisposable InitializeMouseHook(IMouseEvents mouseEvents)
        {
            var anchors = new CompositeDisposable();
            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseDownExt += h,
                    h => mouseEvents.MouseDownExt -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.MouseDown), Log.HandleException)
                .AddTo(anchors);

            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseUpExt += h,
                    h => mouseEvents.MouseUpExt -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.MouseUp), Log.HandleException)
                .AddTo(anchors);

            return anchors;
        }

        private void EnqueueEvent(InputEventData eventData)
        {
            if (Log.IsDebugEnabled)
                Log.Debug(
                    $"Sending event for processing(in queue: {eventQueue.Count}, realtimeMode: {realtimeMode}): {eventData.DumpToTextRaw()}");

            if (realtimeMode)
                ProcessEvent(eventData);
            else
                eventQueue.Add(eventData);
        }

        private void EnqueueEvent(EventArgs args, InputEventType eventType)
        {
            var eventData = new InputEventData {EventArgs = args, EventType = eventType, Timestamp = clock.Now};
            EnqueueEvent(eventData);
        }

        private void LogEvent(object arg)
        {
            if (!Log.IsDebugEnabled) return;

            Log.Debug($"Keyboard/mouse event: {arg.DumpToTextRaw()}");
        }

        private struct InputEventData
        {
            public EventArgs EventArgs { get; set; }

            public InputEventType EventType { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private enum InputEventType
        {
            KeyDown,
            KeyUp,
            KeyPress,
            MouseDown,
            MouseUp
        }
    }
}