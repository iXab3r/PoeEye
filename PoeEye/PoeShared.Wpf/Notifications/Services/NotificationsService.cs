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
using ReactiveUI;
using Unity;

namespace PoeShared.Notifications.Services;

internal sealed class NotificationsService : DisposableReactiveObject, INotificationsService
{
    private static readonly IFluentLog Log = typeof(NotificationsService).PrepareLogger();

    private readonly ISourceList<INotificationContainerViewModel> itemsSource;
    private readonly IFactory<INotificationContainerViewModel, INotificationViewModel> notificationContainerFactory;
    private readonly IOverlayWindowController overlayWindowController;

    public NotificationsService(
        IFactory<IOverlayWindowController, IScheduler> overlayWindowControllerFactory,
        [Dependency(WellKnownSchedulers.UIOverlay)] IScheduler uiOverlayScheduler,
        IFactory<INotificationContainerViewModel, INotificationViewModel> notificationContainerFactory,
        IFactory<OverlayNotificationsContainerViewModel> overlayNotificationsContainerFactory)
    {
        Log.Debug(() => "Initializing notification service");

        this.notificationContainerFactory = notificationContainerFactory;
        overlayWindowController = overlayWindowControllerFactory.Create(uiOverlayScheduler);

        itemsSource = new SourceListEx<INotificationContainerViewModel>().AddTo(Anchors);
        itemsSource
            .Connect()
            .DisposeMany()
            .ObserveOn(uiOverlayScheduler)
            .Bind(out var items)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        Items = items;

        Log.Debug(() => "Sending notification containers creation to UI thread");
        uiOverlayScheduler.Schedule(() =>
        {
            Log.Debug(() => "Preparing notification containers");
            var layeredContainer = overlayNotificationsContainerFactory.Create().AddTo(Anchors);
            layeredContainer.OverlayMode = OverlayMode.Layered;
            overlayWindowController.RegisterChild(layeredContainer).AddTo(Anchors);
            itemsSource
                .Connect()
                .Filter(x => x.Notification.Interactive)
                .ObserveOn(uiOverlayScheduler)
                .Bind(out var interactiveItems)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            layeredContainer.Items = interactiveItems;

            var transparentContainer = overlayNotificationsContainerFactory.Create().AddTo(Anchors);
            transparentContainer.OverlayMode = OverlayMode.Transparent;
            overlayWindowController.RegisterChild(transparentContainer).AddTo(Anchors);
            itemsSource
                .Connect()
                .Filter(x => !x.Notification.Interactive)
                .ObserveOn(uiOverlayScheduler)
                .Bind(out var nonInteractiveItems)
                .SubscribeToErrors(Log.HandleUiException)
                .AddTo(Anchors);
            transparentContainer.Items = nonInteractiveItems;

            layeredContainer.WhenAnyValue(x => x.NativeBounds)
                .ObserveOn(uiOverlayScheduler)
                .SubscribeSafe(containerOffset =>
                {
                    Log.Debug(() => $"Layered container bounds have changed: {containerOffset}, transparent: {transparentContainer.NativeBounds}, offset: {transparentContainer.Offset}");
                    transparentContainer.Offset = new System.Drawing.Point(0, containerOffset.Height + 5);
                }, Log.HandleUiException)
                .AddTo(Anchors);
        });
    }

    public ReadOnlyObservableCollection<INotificationContainerViewModel> Items { get; }

    public void CloseAll()
    {
        itemsSource.Clear();
    }

    public IDisposable AddNotification(INotificationViewModel notification)
    {
        var container = notificationContainerFactory.Create(notification);
        var closeController = new ItemCloseController<INotificationViewModel>(notification, () =>
        {
            Log.Debug(() => $"Removing notification: {notification} in container: {container}");
            itemsSource.Remove(container);
        });
        notification.CloseController = closeController;

        Log.Debug(() => $"Showing notification: {notification} in container: {container}");
        itemsSource.Add(container);
        return container;
    }
}