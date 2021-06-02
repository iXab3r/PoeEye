using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using log4net;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using ReactiveUI;
using Unity;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace PoeShared.Native
{
    internal sealed class OverlayNotificationsContainerViewModel : OverlayViewModelBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OverlayNotificationsContainerViewModel));

        private ReadOnlyObservableCollection<NotificationViewModelBase> items;

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

        public ReadOnlyObservableCollection<NotificationViewModelBase> Items
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

    public abstract class NotificationViewModelBase : DisposableReactiveObject, ICloseable
    {
        private string title;
        private ICloseController closeController;
        private object icon;
        private TimeSpan timeLeft;
        private TimeSpan timeToLive;
        private DateTimeOffset createdAt;

        protected NotificationViewModelBase()
        {
            CloseCommand = CommandWrapper.Create(() => CloseController.Close(), this.WhenAnyValue(x => x.CloseController).Select(x => x != null));
        }

        public ICommand CloseCommand { get; }

        public ICloseController CloseController
        {
            get => closeController;
            set => RaiseAndSetIfChanged(ref closeController, value);
        }

        public string Title
        {
            get => title;
            set => RaiseAndSetIfChanged(ref title, value);
        }

        public object Icon
        {
            get => icon;
            set => RaiseAndSetIfChanged(ref icon, value);
        }

        public DateTimeOffset CreatedAt
        {
            get => createdAt;
            set => RaiseAndSetIfChanged(ref createdAt, value);
        }

        public TimeSpan TimeLeft
        {
            get => timeLeft;
            set => RaiseAndSetIfChanged(ref timeLeft, value);
        }

        public TimeSpan TimeToLive
        {
            get => timeToLive;
            set => RaiseAndSetIfChanged(ref timeToLive, value);
        }
    }

    public sealed class TextNotificationViewModel : NotificationViewModelBase
    {
        private string text;

        public string Text
        {
            get => text;
            set => RaiseAndSetIfChanged(ref text, value);
        }
    }

    public interface INotificationsService
    {
        IDisposable AddNotification(NotificationViewModelBase notification);
    }

    internal sealed class NotificationsService : DisposableReactiveObject, INotificationsService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationsService));

        private readonly OverlayNotificationsContainerViewModel overlayNotificationsContainer;
        private readonly ISourceList<NotificationViewModelBase> itemsSource;

        public NotificationsService(
            [Dependency(WellKnownWindows.AllWindows)] IOverlayWindowController overlayWindowController,
            [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiScheduler,
            OverlayNotificationsContainerViewModel overlayNotificationsContainer)
        {
            this.overlayNotificationsContainer = overlayNotificationsContainer.AddTo(Anchors);
            overlayWindowController.RegisterChild(overlayNotificationsContainer).AddTo(Anchors);
            overlayWindowController.IsEnabled = true;
            
            itemsSource = new SourceList<NotificationViewModelBase>().AddTo(Anchors);
            itemsSource
                .Connect()
                .ObserveOn(uiScheduler)
                .Bind(out var items)
                .Subscribe()
                .AddTo(Anchors);
            overlayNotificationsContainer.Items = Items = items;
        }

        public ReadOnlyObservableCollection<NotificationViewModelBase> Items { get; }

        public IDisposable AddNotification(NotificationViewModelBase notification)
        {
            var anchors = new CompositeDisposable();
            var closeController = new CloseController<NotificationViewModelBase>(notification, () =>
            {
                Log.Debug($"Removing notification: {notification}");
                itemsSource.Remove(notification);
            });
            notification.CloseController = closeController;
            Disposable.Create(() => { closeController.Close(); }).AddTo(anchors);
            notification.WhenAnyValue(x => x.TimeLeft)
                .Subscribe(x => notification.TimeToLive = x)
                .AddTo(anchors);
            
            notification.WhenAnyValue(x => x.TimeToLive)
                .Select(x => x > TimeSpan.Zero ? Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(100)) : Observable.Empty<long>())
                .Switch()
                .Subscribe(() =>
                {
                    if (notification.TimeToLive <= TimeSpan.Zero)
                    {
                        Log.Debug($"Closing notification - timeout {notification.TimeLeft}");
                        closeController.Close();
                    }
                })
                .AddTo(anchors);
            
            Log.Debug($"Showing notification: {notification}");
            itemsSource.Add(notification);
            return anchors;
        }
    }
}