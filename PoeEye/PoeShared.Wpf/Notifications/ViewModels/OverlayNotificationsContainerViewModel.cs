using System.Collections.ObjectModel;
using System.Drawing;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Notifications.ViewModels;

internal sealed class OverlayNotificationsContainerViewModel : OverlayViewModelBase
{
    private static readonly Binder<OverlayNotificationsContainerViewModel> Binder = new();

    static OverlayNotificationsContainerViewModel()
    {
        Binder.Bind(x => x.Items != null && x.Items.Count > 0).To(x => x.IsVisible);
    }
    
    public OverlayNotificationsContainerViewModel()
    {
        Title = "NotificationsContainer";
        SizeToContent = SizeToContent.WidthAndHeight;
        ShowInTaskbar = false;
        IsUnlockable = false;
        EnableHeader = false;

        this.WhenAnyValue(x => x.Offset, x => x.NativeBounds)
            .Select(x => new { Offset, NativeBounds })
            .DistinctUntilChanged()
            .Select(x => new { DesiredBounds = CalculateBounds(x.NativeBounds, x.Offset), x.Offset, x.NativeBounds })
            .SubscribeSafe(x =>
            {
                Log.Debug($"Resizing notification container: {NativeBounds}, params: {x}");
                NativeBounds = x.DesiredBounds;
            }, Log.HandleUiException)
            .AddTo(Anchors);
        
        Binder.Attach(this).AddTo(Anchors);
    }

    public ReadOnlyObservableCollection<INotificationContainerViewModel> Items { get; set; }

    public WinPoint Offset { get; set; }

    private static Rectangle CalculateBounds(Rectangle currentBounds, WinPoint offset)
    {
        //FIXME Show not only on primary
        var primaryMonitorSize = SystemInformation.PrimaryMonitorSize;
        var anchorPoint = new WpfPoint((float)primaryMonitorSize.Width / 2, (float) primaryMonitorSize.Height / 16);
        var anchorOffset = new WpfPoint(- (float)currentBounds.Width / 2, 0);
        var topLeft = new WpfPoint(anchorPoint.X + anchorOffset.X + offset.X, anchorPoint.Y + anchorOffset.Y + offset.Y).ToWinPoint();
        return new Rectangle(topLeft, currentBounds.Size);
    }
}