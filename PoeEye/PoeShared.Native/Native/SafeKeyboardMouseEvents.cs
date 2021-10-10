using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using DynamicData;
using WindowsHook;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoeShared.Prism;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using Unity;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private static readonly IFluentLog Log = typeof(KeyboardEventsSource).PrepareLogger();
        private readonly IClock clock;
        private readonly IScheduler inputScheduler;
        private readonly SourceList<IKeyboardEventFilter> keyboardEventFilters = new();

        private readonly IKeyboardMouseEventsProvider keyboardMouseEventsProvider;
        private readonly SourceList<IMouseEventFilter> mouseEventFilters = new();

        public KeyboardEventsSource(
            IKeyboardMouseEventsProvider keyboardMouseEventsProvider,
            IClock clock,
            [Dependency(WellKnownSchedulers.InputHook)] IScheduler inputScheduler)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Log.Info($"Mouse&keyboard event source initialization started");

            this.keyboardMouseEventsProvider = keyboardMouseEventsProvider;
            this.inputScheduler = inputScheduler;
            this.clock = clock;

            WhenMouseRaw = HookMouseRaw()
                .Publish()
                .RefCount()
                .Where(x => x.EventType == InputEventType.Mouse)
                .Select(x => x.EventArgs as MouseEventExtArgs);
            
            WhenKeyRaw = HookKeyboardRaw()
                .Publish()
                .RefCount()
                .Where(x => x.EventType == InputEventType.Keyboard)
                .Select(x => x.EventArgs as KeyEventArgsExt);

            WhenMouseMove = HookMouseMove()
                .Publish()
                .RefCount()
                .Where(x => x.EventType == InputEventType.MouseMove && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs)
                .DistinctUntilChanged(args => new {args.X, args.Y, args.Button, args.Clicks, args.Delta});

            WhenMouseWheel =
                HookMouseWheel()
                    .Publish()
                    .RefCount()
                    .Where(x => (x.EventType == InputEventType.WheelDown || x.EventType == InputEventType.WheelUp) && x.EventArgs is MouseEventArgs)
                    .Select(x => x.EventArgs as MouseEventArgs);

            var mouseHookSource = HookMouseButtons()
                .Publish()
                .RefCount();

            WhenMouseUp = mouseHookSource
                .Where(x => x.EventType == InputEventType.MouseUp && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs);
            WhenMouseDown = mouseHookSource
                .Where(x => x.EventType == InputEventType.MouseDown && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs);

            var keyboardHook = HookKeyboard()
                .Publish()
                .RefCount();
            WhenKeyDown = keyboardHook
                .Where(x => x.EventType == InputEventType.KeyDown && x.EventArgs is KeyEventArgs)
                .Select(x => x.EventArgs as KeyEventArgs);
            WhenKeyUp = keyboardHook
                .Where(x => x.EventType == InputEventType.KeyUp && x.EventArgs is KeyEventArgs)
                .Select(x => x.EventArgs as KeyEventArgs);
            WhenKeyPress = keyboardHook
                .Where(x => x.EventType == InputEventType.KeyPress && x.EventArgs is KeyPressEventArgs)
                .Select(x => x.EventArgs as KeyPressEventArgs);
        }

        public IObservable<MouseEventExtArgs> WhenMouseRaw { get; }
        
        public IObservable<KeyPressEventArgs> WhenKeyPress { get; }

        public IObservable<KeyEventArgs> WhenKeyDown { get; }

        public IObservable<KeyEventArgs> WhenKeyUp { get; }

        public IObservable<MouseEventArgs> WhenMouseDown { get; }

        public IObservable<MouseEventArgs> WhenMouseMove { get; }

        public IObservable<MouseEventArgs> WhenMouseUp { get; }

        public IObservable<MouseEventArgs> WhenMouseWheel { get; }

        public bool RealtimeMode { get; } = true;
        
        public IObservable<KeyEventArgsExt> WhenKeyRaw { get; }

        public IDisposable AddKeyboardFilter(IKeyboardEventFilter filter)
        {
            Log.Debug($"Adding keyboard filter {filter} to list({mouseEventFilters.Count} items)");
            keyboardEventFilters.Add(filter);
            return Disposable.Create(() =>
            {
                Log.Debug($"Removing keyboard filter {filter} from list({mouseEventFilters.Count} items)");
                keyboardEventFilters.Remove(filter);
            });
        }

        public IDisposable AddMouseFilter(IMouseEventFilter filter)
        {
            Log.Debug($"Adding  mouse filter {filter} to list({mouseEventFilters.Count} items)");
            mouseEventFilters.Add(filter);
            return Disposable.Create(() =>
            {
                Log.Debug($"Removing mouse filter {filter} from list({mouseEventFilters.Count} items)");
                mouseEventFilters.Remove(filter);
            });
        }

        private IObservable<InputEventData> HookMouseButtons()
        {
            return PrepareHook("MouseButtons", keyboardMouseEventsProvider.System, InitializeMouseButtonsHook, ShouldProcess, inputScheduler);
        }

        private IObservable<InputEventData> HookMouseWheel()
        {
            return PrepareHook("MouseWheel", keyboardMouseEventsProvider.System, InitializeMouseWheelHook, ShouldProcess, inputScheduler);
        }

        private IObservable<InputEventData> HookMouseMove()
        {
            return PrepareHook("MouseMove", keyboardMouseEventsProvider.System, InitializeMouseMoveHook, ShouldProcess, inputScheduler);
        }

        private IObservable<InputEventData> HookKeyboard()
        {
            return PrepareHook("Keyboard", keyboardMouseEventsProvider.System, InitializeKeyboardHook, ShouldProcess, inputScheduler);
        }
        
        private IObservable<InputEventData> HookKeyboardRaw()
        {
            return PrepareHook("KeyboardRaw", keyboardMouseEventsProvider.System, InitializeKeyboardRaw, ShouldProcess, inputScheduler);
        }
        
        private IObservable<InputEventData> HookMouseRaw()
        {
            return PrepareHook("MouseRaw", keyboardMouseEventsProvider.System, InitializeMouseRaw, ShouldProcess, inputScheduler);
        }

        private static IObservable<InputEventData> PrepareHook(
            string hookName,
            IObservable<IKeyboardMouseEvents> keyboardMouseEvents,
            Func<IKeyboardMouseEvents, IObservable<InputEventData>> hookMethod,
            Predicate<InputEventData> filter,
            IScheduler scheduler)
        {
            return Observable.Create<InputEventData>(subscriber =>
            {
                Log.Info($"[{hookName}] Configuring subscription...");
                var sw = Stopwatch.StartNew();
                var activeAnchors = new CompositeDisposable();
                Disposable.Create(() => Log.Info($"[{hookName}] Unsubscribing")).AddTo(activeAnchors);

                Log.Info($"Sending subscription to {scheduler}");

                var result = new Subject<InputEventData>();
                result.Subscribe(subscriber).AddTo(activeAnchors);
                
                scheduler.Schedule(() =>
                {
                    Log.Info($"[{hookName}] Subscribing...");
                    keyboardMouseEvents
                        .Select(hookMethod)
                        .Switch()
                        .Do(LogEvent, Log.HandleException, () => Log.Debug($"{hookName} event loop completed"))
                        .Where(x => filter(x))
                        .Subscribe(result)
                        .AddTo(activeAnchors);
                    sw.Stop();
                    Log.Info($"[{hookName}] Configuration took {sw.ElapsedMilliseconds:F0}ms");
                }).AddTo(activeAnchors);

                return activeAnchors;
            });
        }

        private IObservable<InputEventData> InitializeKeyboardHook(IKeyboardEvents keyboardEvents)
        {
            Log.Info($"Hooking Keyboard using {keyboardEvents}");
            var keyDown = Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardEvents.KeyDown += h,
                    h => keyboardEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.KeyDown));

            var keyUp = Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardEvents.KeyUp += h,
                    h => keyboardEvents.KeyUp -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.KeyUp));

            var keyPress = Observable
                .FromEventPattern<KeyPressEventHandler, KeyPressEventArgs>(
                    h => keyboardEvents.KeyPress += h,
                    h => keyboardEvents.KeyPress -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.KeyPress));

            return Observable.Merge(keyDown, keyUp, keyPress);
        }

        private IObservable<InputEventData> InitializeMouseButtonsHook(IMouseEvents mouseEvents)
        {
            Log.Info($"Hooking Mouse buttons using {mouseEvents}");

            var mouseDown = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseDownExt += h,
                    h => mouseEvents.MouseDownExt -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.MouseDown));

            var mouseUp = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseUpExt += h,
                    h => mouseEvents.MouseUpExt -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.MouseUp));

            return Observable.Merge(mouseDown, mouseUp);
        }

        private IObservable<InputEventData> InitializeMouseWheelHook(IMouseEvents mouseEvents)
        {
            Log.Info($"Hooking Mouse Wheel using {mouseEvents}");

            var mouseWheel = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseWheelExt += h,
                    h => mouseEvents.MouseWheelExt -= h)
                .Select(x => x.EventArgs)
                .Where(x => x.WheelScrolled)
                .Select(x => ToInputEventData(x, x.Delta > 0 ? InputEventType.WheelDown : InputEventType.WheelUp));

            return mouseWheel;
        }

        private IObservable<InputEventData> InitializeMouseMoveHook(IMouseEvents mouseEvents)
        {
            Log.Info($"Hooking Mouse Move (possible performance hit !) using {mouseEvents}");

            var mouseMove = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseMoveExt += h,
                    h => mouseEvents.MouseMoveExt -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.MouseMove));

            return mouseMove;
        }
        
        private IObservable<InputEventData> InitializeMouseRaw(IMouseEvents mouseEvents)
        {
            Log.Info($"Hooking Mouse Raw (possible performance hit !) using {mouseEvents}");

            var mouseMove = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseRaw += h,
                    h => mouseEvents.MouseRaw -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.Mouse));

            return mouseMove;
        }
        
        private IObservable<InputEventData> InitializeKeyboardRaw(IKeyboardEvents keyboardEvents)
        {
            Log.Info($"Hooking Keyboard Raw (possible performance hit !) using {keyboardEvents}");

            var mouseMove = Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardEvents.KeyRaw += h,
                    h => keyboardEvents.KeyRaw -= h)
                .Select(x => x.EventArgs)
                .Select(x => ToInputEventData(x, InputEventType.Keyboard));

            return mouseMove;
        }

        private InputEventData ToInputEventData(EventArgs args, InputEventType eventType)
        {
            return new InputEventData {EventArgs = args, EventType = eventType, Timestamp = clock.Now};
        }

        private bool ShouldProcess(InputEventData inputEventData)
        {
            var result = inputEventData.EventArgs switch
            {
                KeyEventArgsExt keyEventArgs => keyboardEventFilters.Count <= 0 || keyboardEventFilters.Items.All(x => x.ShouldProcess(keyEventArgs)),
                MouseEventExtArgs mouseEventArgs => mouseEventFilters.Count <= 0 || mouseEventFilters.Items.All(x => x.ShouldProcess(mouseEventArgs)),
                _ => true
            };

            if (!result && Log.IsDebugEnabled)
            {
                Log.Debug($"Input event data {inputEventData} is filtered");
            }

            return result;
        }

        private static void LogEvent(InputEventData arg)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Keyboard/Mouse event: {arg.DumpToTextRaw()}");
            }
        }

        private struct InputEventData
        {
            [JsonConverter(typeof(StringEnumConverter))] public InputEventType EventType { get; set; }

            public EventArgs EventArgs { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private enum InputEventType
        {
            Keyboard,
            KeyDown,
            KeyUp,
            KeyPress,
            Mouse,
            MouseDown,
            MouseUp,
            MouseMove,
            WheelDown,
            WheelUp
        }
    }
}