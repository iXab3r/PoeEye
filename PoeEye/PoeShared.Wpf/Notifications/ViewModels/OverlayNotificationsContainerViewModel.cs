using System.Collections.ObjectModel;
using System.Drawing;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using ReactiveUI;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Notifications.ViewModels
{
    internal sealed class OverlayNotificationsContainerViewModel : OverlayViewModelBase
    {
        private static readonly IFluentLog Log = typeof(OverlayNotificationsContainerViewModel).PrepareLogger();

        private ReadOnlyObservableCollection<INotificationContainerViewModel> items;

        public OverlayNotificationsContainerViewModel()
        {
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;
            IsUnlockable = false;
            OverlayMode = OverlayMode.Transparent;

            this.WhenAnyValue(x => x.ActualWidth, x => x.ActualHeight)
                .Select(x => new Size(ActualWidth, ActualHeight))
                .DistinctUntilChanged()
                .ObserveOnDispatcher()
                .SubscribeSafe(HandleSizeChange, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<INotificationContainerViewModel> Items
        {
            get => items;
            set => RaiseAndSetIfChanged(ref items, value);
        }

        private void HandleSizeChange(Size actualSize)
        {
            var primaryMonitorSize = SystemInformation.PrimaryMonitorSize;

            var anchorPoint = new Point((float)primaryMonitorSize.Width / 2, 0);
            var offset = new Point(- (float)actualSize.Width / 2, 0);
            var topLeft = new Point(anchorPoint.X + offset.X, anchorPoint.Y + offset.Y).ToWinPoint();
            NativeBounds = new Rectangle(topLeft, NativeBounds.Size);
        }
    }
}