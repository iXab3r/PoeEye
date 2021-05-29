using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using DynamicData;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.UI.Hotkeys
{
    internal sealed class HotkeySequenceActions : HotkeySequenceItem
    {
        private readonly HotkeySequenceEditor editor;
        private readonly SourceList<HotkeySequenceItem> itemsSource;
        private readonly IKeyboardEventsSource eventsSource;
        private IKeyboardEventsSource keyboardEventSource;
        private bool isRecording;

        public HotkeySequenceActions(
            HotkeySequenceEditor editor,
            SourceList<HotkeySequenceItem> itemsSource)
        {
            this.editor = editor;
            this.itemsSource = itemsSource;
            AddDelayItem = CommandWrapper.Create(() => new HotkeySequenceDelay(editor).AddTo(itemsSource));
            AddTextItem = CommandWrapper.Create(() => new HotkeySequenceText() {  Text = "text" }.AddTo(itemsSource));
            AddRecording = CommandWrapper.Create(AddRecordingExecuted);
        }

        private async Task AddRecordingExecuted()
        {
            if (keyboardEventSource == null)
            {
                throw new InvalidOperationException("Keyboard event source is not initialized");
            }

            var cancel = Observable.Merge(
                keyboardEventSource.WhenKeyDown.Where(x => x.KeyCode == Keys.Escape)
            );

            var sw = Stopwatch.StartNew();
            
            Observable.Merge(
                    keyboardEventSource.WhenKeyDown.Select(x => new { x.KeyCode, IsDown = true }),
                    keyboardEventSource.WhenKeyUp.Select(x => new { x.KeyCode, IsDown = false })
                )
                .TakeUntil(cancel)
                .DistinctUntilChanged()
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    new HotkeySequenceDelay(editor)
                    {
                        Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                        IsKeypress = true,
                    }.AddTo(itemsSource);
                    sw.Restart();
                    new HotkeySequenceHotkey()
                    {
                        Hotkey = new HotkeyGesture(x.KeyCode.ToInputKey()),
                        IsDown = x.IsDown,
                    }.AddTo(itemsSource);
                    sw.Restart();
                });
            
            Observable.Merge(
                    keyboardEventSource.WhenMouseDown.Select(x => new { x.Button, x.X, x.Y, IsDown = true }),
                    keyboardEventSource.WhenMouseUp.Select(x => new { x.Button,  x.X, x.Y, IsDown = false })
                )
                .TakeUntil(cancel)
                .DistinctUntilChanged()
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    new HotkeySequenceDelay(editor)
                    {
                        Delay = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds),
                        IsKeypress = true,
                    }.AddTo(itemsSource);
                    sw.Restart();
                    new HotkeySequenceHotkey()
                    {
                        Hotkey = new HotkeyGesture(x.Button),
                        IsDown = x.IsDown,
                    }.AddTo(itemsSource);
                    sw.Restart();
                });
        }

        public IKeyboardEventsSource KeyboardEventSource
        {
            get => keyboardEventSource;
            set => RaiseAndSetIfChanged(ref keyboardEventSource, value);
        }


        public bool IsRecording
        {
            get => isRecording;
            set => RaiseAndSetIfChanged(ref isRecording, value);
        }

        public override bool IsDragDropSource => false;

        public ICommand AddRecording { get; }
        public ICommand AddDelayItem { get; }
        public ICommand AddTextItem { get; }
    }
}