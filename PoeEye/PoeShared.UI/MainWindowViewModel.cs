using System;
using System.Drawing;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic.Logging;
using PoeShared.Audio.ViewModels;
using PoeShared.Native;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Notifications;
using PoeShared.Scaffolding.WPF;
using PoeShared.Wpf.Scaffolding;

namespace PoeShared.UI
{
    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        private DisposableReactiveObject fakeDelay;
        private TimeSpan randomPeriod;
        private Rect selectionRect;

        private Rectangle selectionRectangle;

        public MainWindowViewModel(
            IAudioNotificationSelectorViewModel audioNotificationSelector,
            IRandomPeriodSelector randomPeriodSelector,
            ISelectionAdornerViewModel selectionAdorner,
            NotificationSandboxViewModel notificationSandbox,
            IHotkeySequenceEditorViewModel hotkeySequenceEditor)
        {
            NotificationSandbox = notificationSandbox;
            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            AudioNotificationSelector = audioNotificationSelector.AddTo(Anchors);
            RandomPeriodSelector = randomPeriodSelector.AddTo(Anchors);
            HotkeySequenceEditor = hotkeySequenceEditor.AddTo(Anchors);
            LongCommand = CommandWrapper.Create(async () =>
            {
                await Task.Delay(3000);
            });
            
            ErrorCommand = CommandWrapper.Create(async () =>
            {
                await Task.Delay(3000);
                throw new ApplicationException("Error");
            });
            
            RandomPeriodSelector.LowerValue = TimeSpan.FromSeconds(3);
            RandomPeriodSelector.UpperValue = TimeSpan.FromSeconds(3);
            NextRandomPeriodCommand = CommandWrapper.Create(() => RandomPeriod = randomPeriodSelector.GetValue());
            StartSelectionCommand = CommandWrapper.Create(HandleSelectionCommandExecuted);
            SetCachedControlContentCommand = CommandWrapper.Create<object>(arg =>
            {
                if (arg is string name)
                {
                    FakeDelay = new FakeDelayStringViewModel() { Name = name };
                }
                else if (arg is int num)
                {
                    FakeDelay = new FakeDelayNumberViewModel() { Number = num };
                }
                else
                {
                    FakeDelay = null;
                }
            });
        }

        public NotificationSandboxViewModel NotificationSandbox { get; }

        public ICommand StartSelectionCommand { get; }


        public Rectangle SelectionRectangle
        {
            get => selectionRectangle;
            set => RaiseAndSetIfChanged(ref selectionRectangle, value);
        }

        public Rect SelectionRect
        {
            get => selectionRect;
            set => RaiseAndSetIfChanged(ref selectionRect, value);
        }

        public ISelectionAdornerViewModel SelectionAdorner { get; }

        public IAudioNotificationSelectorViewModel AudioNotificationSelector { get; }

        public IRandomPeriodSelector RandomPeriodSelector { get; }

        public IHotkeySequenceEditorViewModel HotkeySequenceEditor { get; }

        public CommandWrapper LongCommand { get; }

        public CommandWrapper ErrorCommand { get; }

        public ICommand NextRandomPeriodCommand { get; }

        public ICommand SetCachedControlContentCommand { get; }

        public DisposableReactiveObject FakeDelay
        {
            get => fakeDelay;
            set => RaiseAndSetIfChanged(ref fakeDelay, value);
        }

        public TimeSpan RandomPeriod
        {
            get => randomPeriod;
            set => RaiseAndSetIfChanged(ref randomPeriod, value);
        }

        private async Task HandleSelectionCommandExecuted()
        {
            var selection = await SelectionAdorner.StartSelection()
                .Take(1);
            SelectionRect = selection;
        }
    }
}