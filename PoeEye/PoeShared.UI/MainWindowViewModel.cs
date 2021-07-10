using System;
using System.Drawing;
using System.Linq;
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
using PoeShared.Scaffolding.WPF;
using PoeShared.Wpf.Scaffolding;

namespace PoeShared.UI
{
    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        private readonly INotificationsService notificationsService;
        private TimeSpan randomPeriod;

        private Rectangle selectionRectangle;
        private Rect selectionRect;

        public MainWindowViewModel(
            IAudioNotificationSelectorViewModel audioNotificationSelector,
            IRandomPeriodSelector randomPeriodSelector,
            INotificationsService notificationsService,
            ISelectionAdornerViewModel selectionAdorner,
            IHotkeySequenceEditorViewModel hotkeySequenceEditor)
        {
            SelectionAdorner = selectionAdorner.AddTo(Anchors);
            this.notificationsService = notificationsService;
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
            
            AddTextNotification = CommandWrapper.Create(AddTextNotificationExecuted);
            RandomPeriodSelector.LowerValue = TimeSpan.FromSeconds(3);
            RandomPeriodSelector.UpperValue = TimeSpan.FromSeconds(3);
            NextRandomPeriodCommand = CommandWrapper.Create(() => RandomPeriod = randomPeriodSelector.GetValue());
            StartSelectionCommand = CommandWrapper.Create(HandleSelectionCommandExecuted);
        }

        public ICommand StartSelectionCommand { get; }

        private void AddTextNotificationExecuted()
        {
            var rng = new Random();
            var notification = new TextNotificationViewModel()
            {
                Text = Enumerable.Repeat("a", (int)rng.Next(10, 60)).JoinStrings(" "),
                TimeLeft = NotificationTimeout
            };

            notificationsService.AddNotification(notification);
        }
        
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
        
        public CommandWrapper AddTextNotification { get; }

        private TimeSpan notificationTimeout = TimeSpan.Zero;

        public TimeSpan NotificationTimeout
        {
            get => notificationTimeout;
            set => RaiseAndSetIfChanged(ref notificationTimeout, value);
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