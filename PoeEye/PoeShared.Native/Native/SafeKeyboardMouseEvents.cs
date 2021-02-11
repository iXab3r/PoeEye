using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using JetBrains.Annotations;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PoeShared.Scaffolding;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace PoeShared.Native
{
    internal sealed class KeyboardEventsSource : DisposableReactiveObject, IKeyboardEventsSource
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(KeyboardEventsSource));
        
        private readonly IClock clock;

        public KeyboardEventsSource([NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            Log.Info("Mouse&keyboard event source initialization started");

            this.clock = clock;
            
            WhenMouseMove = Observable
                .Using(Hook.GlobalEvents, HookMouseMove)
                .Publish()
                .RefCount()
                .Where(x => x.EventType == InputEventType.MouseMove && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs)
                .DistinctUntilChanged(args => new { args.X, args.Y, args.Button, args.Clicks, args.Delta });
            
            WhenMouseWheel = Observable
                .Using(Hook.GlobalEvents, HookMouseWheel)
                .Publish()
                .RefCount()
                .Where(x => (x.EventType == InputEventType.WheelDown || x.EventType == InputEventType.WheelUp) && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs);
            
            var mouseHookSource = Observable
                .Using(Hook.GlobalEvents, HookMouseButtons)
                .Publish()
                .RefCount();
            
            WhenMouseUp = mouseHookSource
                .Where(x => x.EventType == InputEventType.MouseUp && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs);
            WhenMouseDown = mouseHookSource
                .Where(x => x.EventType == InputEventType.MouseDown && x.EventArgs is MouseEventArgs)
                .Select(x => x.EventArgs as MouseEventArgs);
            
            var keyboardHook = Observable
                .Using(Hook.GlobalEvents, HookKeyboard)
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

        public IObservable<KeyPressEventArgs> WhenKeyPress { get; }

        public IObservable<KeyEventArgs> WhenKeyDown { get; }

        public IObservable<KeyEventArgs> WhenKeyUp { get; }

        public IObservable<MouseEventArgs> WhenMouseDown { get; }

        public IObservable<MouseEventArgs> WhenMouseMove { get; }

        public IObservable<MouseEventArgs> WhenMouseUp { get; }
        
        public IObservable<MouseEventArgs> WhenMouseWheel { get; }

        public bool RealtimeMode { get; } = true;

        private IObservable<InputEventData> HookMouseButtons(IKeyboardMouseEvents source)
        {
            return PrepareHook( "MouseButtons", () => InitializeMouseButtonsHook(source));
        }
        
        private IObservable<InputEventData> HookMouseWheel(IKeyboardMouseEvents source)
        {
            return PrepareHook( "MouseWheel", () => InitializeMouseWheelHook(source));
        }
        
        private IObservable<InputEventData> HookMouseMove(IKeyboardMouseEvents source)
        {
            return PrepareHook( "MouseMove", () => InitializeMouseMoveHook(source));
        }
        
        private IObservable<InputEventData> HookKeyboard(IKeyboardMouseEvents source)
        {
            return PrepareHook( "Keyboard", () => InitializeKeyboardHook(source));
        }
        
        private static IObservable<InputEventData> PrepareHook(string hookName, Func<IObservable<InputEventData>> hookMethod)
        {
            return Observable.Create<InputEventData>(subscriber =>
            {
                Log.Info($"Configuring {hookName} hook...");
                var sw = Stopwatch.StartNew();
                var activeAnchors = new CompositeDisposable();
                Disposable.Create(() => Log.Info($"Disposing {hookName} hook")).AddTo(activeAnchors);

                hookMethod()
                    .Do(LogEvent, Log.HandleException, () => Log.Debug($"{hookName} event loop completed"))
                    .Subscribe(subscriber)
                    .AddTo(activeAnchors);
                
                sw.Stop();
                Log.Debug($"{hookName} hook configuration took {sw.ElapsedMilliseconds:F0}ms");
                
                return activeAnchors;
            });
        }
        
        private IObservable<InputEventData> InitializeKeyboardHook(IKeyboardEvents keyboardEvents)
        {
            Log.Debug($"Hooking Keyboard: {keyboardEvents}");
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
            Log.Debug($"Hooking Mouse buttons: {mouseEvents}");

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
            Log.Debug($"Hooking Mouse buttons: {mouseEvents}");

            var mouseWheel = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseWheelExt += h,
                    h => mouseEvents.MouseWheelExt -= h)
                .Select(x => x.EventArgs)
                .Where(x =>x.WheelScrolled)
                .Select(x => ToInputEventData(x, x.Delta > 0 ? InputEventType.WheelDown : InputEventType.WheelUp));

            return mouseWheel;
        }

        private IObservable<InputEventData> InitializeMouseMoveHook(IMouseEvents mouseEvents)
        {
            Log.Debug($"Hooking Mouse Move (possible performance hit !): {mouseEvents}");

            var mouseMove = Observable
                .FromEventPattern<EventHandler<MouseEventExtArgs>, MouseEventExtArgs>(
                    h => mouseEvents.MouseMoveExt += h,
                    h => mouseEvents.MouseMoveExt -= h)
                .Select(x => x.EventArgs)
                .Select(EnrichMouseMove)
                .Select(x => ToInputEventData(x, InputEventType.MouseMove));
            
            return mouseMove;
        }
        
        private InputEventData ToInputEventData(EventArgs args, InputEventType eventType)
        {
            return new InputEventData {EventArgs = args, EventType = eventType, Timestamp = clock.Now};
        }
        
        private static void LogEvent(InputEventData arg)
        {
            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Keyboard/Mouse event: {arg.DumpToTextRaw()}");
            }
        }

        private static MouseEventArgs EnrichMouseMove(MouseEventExtArgs args)
        {
            return new MouseEventArgs(Control.MouseButtons, args.Clicks, args.X, args.Y, args.Delta);
        }

        private struct InputEventData
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public InputEventType EventType { get; set; }
            
            public EventArgs EventArgs { get; set; }

            public DateTime Timestamp { get; set; }
        }

        private enum InputEventType
        {
            KeyDown,
            KeyUp,
            KeyPress,
            MouseDown,
            MouseUp,
            MouseMove,
            WheelDown,
            WheelUp
        }
    }
}