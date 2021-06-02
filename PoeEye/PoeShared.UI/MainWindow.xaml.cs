using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI.Hotkeys;

namespace PoeShared.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    internal sealed class MainWindowViewModel : DisposableReactiveObject
    {
        private readonly INotificationsService notificationsService;

        public MainWindowViewModel(
            INotificationsService notificationsService,
            IHotkeySequenceEditorViewModel hotkeySequenceEditor)
        {
            this.notificationsService = notificationsService;
            HotkeySequenceEditor = hotkeySequenceEditor;
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
        }

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

        public IHotkeySequenceEditorViewModel HotkeySequenceEditor { get; }

        public CommandWrapper LongCommand { get; }
        
        public CommandWrapper ErrorCommand { get; }
        
        public CommandWrapper AddTextNotification { get; }

        private TimeSpan notificationTimeout = TimeSpan.Zero;

        public TimeSpan NotificationTimeout
        {
            get => notificationTimeout;
            set => RaiseAndSetIfChanged(ref notificationTimeout, value);
        }
    }
}