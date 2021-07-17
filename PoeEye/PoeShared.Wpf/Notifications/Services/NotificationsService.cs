using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Notifications.ViewModels;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Unity;

namespace PoeShared.Notifications.Services
{
    internal sealed class NotificationsService : DisposableReactiveObject, INotificationsService
    {
        private static readonly IFluentLog Log = typeof(NotificationsService).PrepareLogger();

        private readonly ISourceList<INotificationContainerViewModel> itemsSource;
        private readonly IFactory<INotificationContainerViewModel, INotificationViewModel> notificationContainerFactory;

        public NotificationsService(
            [Dependency(WellKnownWindows.AllWindows)] IOverlayWindowController overlayWindowController,
            [Dependency(WellKnownSchedulers.UIIdle)] IScheduler uiScheduler,
            IFactory<INotificationContainerViewModel, INotificationViewModel> notificationContainerFactory,
            OverlayNotificationsContainerViewModel overlayNotificationsContainer)
        {
            this.notificationContainerFactory = notificationContainerFactory;
            overlayNotificationsContainer.AddTo(Anchors);
            overlayWindowController.RegisterChild(overlayNotificationsContainer).AddTo(Anchors);
            overlayWindowController.IsEnabled = true;
            
            itemsSource = new SourceList<INotificationContainerViewModel>().AddTo(Anchors);
            itemsSource
                .Connect()
                .DisposeMany()
                .ObserveOn(uiScheduler)
                .Bind(out var items)
                .Subscribe()
                .AddTo(Anchors);
            overlayNotificationsContainer.Items = Items = items;
        }

        public ReadOnlyObservableCollection<INotificationContainerViewModel> Items { get; }

        public void CloseAll()
        {
            itemsSource.Clear();
        }

        public IDisposable AddNotification(INotificationViewModel notification)
        {
            var container = notificationContainerFactory.Create(notification);
            var closeController = new CloseController<INotificationViewModel>(notification, () =>
            {
                Log.Debug($"Removing notification: {notification} in container: {container}");
                itemsSource.Remove(container);
            });
            notification.CloseController = closeController;
            
            Log.Debug($"Showing notification: {notification} in container: {container}");
            itemsSource.Add(container);
            return container;
        }
    }
}