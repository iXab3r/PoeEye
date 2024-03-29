using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Notifications.ViewModels;

internal sealed class NotificationContainerViewModel : DisposableReactiveObject, INotificationContainerViewModel
{
    private static readonly IFluentLog Log = typeof(NotificationContainerViewModel).PrepareLogger();
    private static readonly Binder<NotificationContainerViewModel> Binder = new();
    private readonly IClock clock;
    private readonly Dispatcher uiDispatcher;

    static NotificationContainerViewModel()
    {
        Binder.Bind(x => x.Notification.Icon).To((x, v) => x.Icon = v?.ToBitmapSource(), x => x.uiDispatcher);
        Binder.Bind(x => x.Notification.Title).To((x, v) => x.Title = v, x => x.uiDispatcher);
        Binder.Bind(x => x.clock.UtcNow)
            .WithDependency(x => x.Notification.TimeToLive)
            .To((x, v) => x.TimeToLiveChangeTimestamp = v);
    }

    public NotificationContainerViewModel(
        IClock clock,
        INotificationViewModel notification)
    {
        uiDispatcher = Dispatcher.CurrentDispatcher;
        CreatedAt = clock.UtcNow;
        this.clock = clock;
        Notification = notification;
        CloseCommand = CommandWrapper.Create(() => notification.CloseController.Close(), notification.WhenAnyValue(x => x.CloseController).Select(x => x != null));

        notification.WhenAnyValue(x => x.TimeToLive)
            .Select(x => x > TimeSpan.Zero ? Observables.BlockingTimer(UiConstants.UiThrottlingDelay) : Observable.Empty<long>())
            .Switch()
            .ObserveOnCurrentDispatcherIfNeeded()
            .Subscribe(() =>
            {
                var elapsed = clock.UtcNow - TimeToLiveChangeTimestamp;
                TimeLeft = (notification.TimeToLive - elapsed).EnsureInRange(TimeSpan.Zero, TimeSpan.MaxValue);
                    
                if (TimeLeft <= TimeSpan.Zero)
                {
                    Log.Debug($"Closing notification - timeout {TimeLeft}");
                    notification.CloseController.Close();
                }
            })
            .AddTo(Anchors);

        Observable.CombineLatest(
                notification.WhenAnyValue(x => x.TimeToLive).ToUnit(),
                this.WhenAnyValue(x => x.TimeLeft).ToUnit())
            .Select(_ =>
            {
                if (notification.TimeToLive <= TimeSpan.Zero)
                {
                    return 1;
                }
                var progress = TimeLeft.TotalMilliseconds / notification.TimeToLive.TotalMilliseconds;
                if (progress > 0.5)
                {
                    return 1;
                }

                return progress * 2;
            })
            .ObserveOnCurrentDispatcherIfNeeded()
            .SubscribeSafe(x => Opacity = x, Log.HandleUiException)
            .AddTo(Anchors);
            
        Disposable.Create(() => { notification.CloseController.Close(); }).AddTo(Anchors);
        Binder.Attach(this).AddTo(Anchors);
    }

    public INotificationViewModel Notification { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset TimeToLiveChangeTimestamp { get; private set; }

    public TimeSpan TimeLeft { get; private set; }

    public string Title { get; private set; }

    public ImageSource Icon { get; private set; }

    public double Opacity { get; set; } = 1;

    public ICommand CloseCommand { get; }
}