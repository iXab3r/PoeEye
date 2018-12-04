using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private readonly IKeyboardMouseEvents keyboardMouseEvents;
        private readonly IClock clock;
        private readonly ISubject<KeyEventArgs> whenKeyDown = new Subject<KeyEventArgs>();

        private readonly ISubject<KeyPressEventArgs> whenKeyPress = new Subject<KeyPressEventArgs>();
        private readonly ISubject<KeyEventArgs> whenKeyUp = new Subject<KeyEventArgs>();
        private readonly ISubject<MouseEventExtArgs> whenMouseDown = new Subject<MouseEventExtArgs>();
        private readonly ISubject<MouseEventExtArgs> whenMouseUp = new Subject<MouseEventExtArgs>();
        
        private readonly BlockingCollection<InputEventData> eventQueue = new BlockingCollection<InputEventData>();

        public KeyboardEventsSource(
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(clock, nameof(clock));
            this.keyboardMouseEvents = keyboardMouseEvents;
            this.clock = clock;

            InitializeHook();
            InitializeConsumer().AddTo(Anchors);
        }

        public IObservable<KeyPressEventArgs> WhenKeyPress => whenKeyPress;

        public IObservable<KeyEventArgs> WhenKeyDown => whenKeyDown;

        public IObservable<KeyEventArgs> WhenKeyUp => whenKeyUp;
        public IObservable<MouseEventArgs> WhenMouseDown => whenMouseDown.OfType<MouseEventArgs>();
        public IObservable<MouseEventArgs> WhenMouseUp => whenMouseUp.OfType<MouseEventArgs>();

        private IDisposable InitializeConsumer()
        {
            Log.Debug($"Creating new event consumer thread");
            var consumer = new CancellationTokenSource();
            var consumerThread = new Thread(InitializeConsumerThread)
            {
                Name = $"Input",
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
            var consumer = (CancellationToken)arg;
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
                    switch (nextEvent.EventType)
                    {
                        case InputEventType.KeyDown:
                            whenKeyDown.OnNext((KeyEventArgs)nextEvent.EventArgs);
                            break;
                        case InputEventType.KeyUp:
                            whenKeyUp.OnNext((KeyEventArgs)nextEvent.EventArgs);
                            break;
                        case InputEventType.KeyPress:
                            whenKeyPress.OnNext((KeyPressEventArgs)nextEvent.EventArgs);
                            break;
                        case InputEventType.MouseDown:
                            whenMouseDown.OnNext((MouseEventExtArgs)nextEvent.EventArgs);
                            break;
                        case InputEventType.MouseUp:
                            whenMouseUp.OnNext((MouseEventExtArgs)nextEvent.EventArgs);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(nextEvent), nextEvent.EventType, $"Invalid enum value, data: {nextEvent.DumpToTextRaw()}");
                    }
                }
            }
            catch (OperationCanceledException e)
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

        private void InitializeHook()
        {
            Log.Debug($"Hook: {keyboardMouseEvents}");
            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyDown), Log.HandleException)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyUp += h,
                    h => keyboardMouseEvents.KeyUp -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyUp), Log.HandleException)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(
                    h => keyboardMouseEvents.KeyPress += h,
                    h => keyboardMouseEvents.KeyPress -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.KeyPress), Log.HandleException)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => keyboardMouseEvents.MouseDownExt += h,
                    h => keyboardMouseEvents.MouseDownExt -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.MouseDown), Log.HandleException)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => keyboardMouseEvents.MouseUpExt += h,
                    h => keyboardMouseEvents.MouseUpExt -= h)
                .Select(x => x.EventArgs)
                .Do(LogEvent)
                .Subscribe(x => EnqueueEvent(x, InputEventType.MouseUp), Log.HandleException)
                .AddTo(Anchors);
        }

        private void EnqueueEvent(EventArgs args, InputEventType eventType)
        {
            var eventData = new InputEventData() {EventArgs = args, EventType = eventType, Timestamp = clock.Now};
            if (Log.IsTraceEnabled)
            {
                Log.Trace($"Sending event for processing(in queue: {eventQueue.Count}): {eventData.DumpToTextRaw()}");
            }
            eventQueue.Add(eventData);
        }

        private void LogEvent(object arg)
        {
            if (!Log.IsTraceEnabled)
            {
                return;
            }

            Log.Trace($"Keyboard/mouse event: {arg.DumpToTextRaw()}");
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