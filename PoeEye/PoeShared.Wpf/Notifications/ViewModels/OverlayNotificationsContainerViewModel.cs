using System;
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
using WinPoint = System.Drawing.Point;
using WinSize = System.Drawing.Size;

namespace PoeShared.Notifications.ViewModels
{
    internal sealed class OverlayNotificationsContainerViewModel : OverlayViewModelBase
    {
        private static readonly IFluentLog Log = typeof(OverlayNotificationsContainerViewModel).PrepareLogger();

        private ReadOnlyObservableCollection<INotificationContainerViewModel> items;

        private WinPoint offset;

        public OverlayNotificationsContainerViewModel()
        {
            Title = "NotificationsContainer";
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;
            IsUnlockable = false;
            EnableHeader = false;

            this.WhenAnyValue(x => x.ActualWidth, x => x.ActualHeight, x => x.Offset, x => x.NativeBounds)
                .Select(x => new { ActualSize = new Size(ActualWidth, ActualHeight).ScaleToScreen(Dpi), Offset, NativeBounds })
                .DistinctUntilChanged()
                .ObserveOnDispatcher()
                .Select(x => CalculateBounds(x.NativeBounds, x.ActualSize, x.Offset))
                .SubscribeSafe(x => NativeBounds = x, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public ReadOnlyObservableCollection<INotificationContainerViewModel> Items
        {
            get => items;
            set => RaiseAndSetIfChanged(ref items, value);
        }

        public WinPoint Offset
        {
            get => offset;
            set => RaiseAndSetIfChanged(ref offset, value);
        }

        private static Rectangle CalculateBounds(Rectangle currentBounds, WinSize actualSize, WinPoint offset)
        {
            var primaryMonitorSize = SystemInformation.PrimaryMonitorSize;

            var anchorPoint = new Point((float)primaryMonitorSize.Width / 2, (float) primaryMonitorSize.Height / 16);
            var anchorOffset = new Point(- (float)actualSize.Width / 2, 0);
            var topLeft = new Point(anchorPoint.X + anchorOffset.X + offset.X, anchorPoint.Y + anchorOffset.Y + offset.Y).ToWinPoint();

            return new Rectangle(topLeft, currentBounds.Size);
        }
    }
}