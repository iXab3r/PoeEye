using System;
using System.Windows.Media;
using PoeShared.Scaffolding;
using PoeShared.Services;

namespace PoeShared.Notifications.ViewModels;

public abstract class NotificationViewModelBase : DisposableReactiveObject, INotificationViewModel
{

    protected NotificationViewModelBase()
    {
    }

    public bool Interactive { get; set; } = true;

    public ICloseController CloseController { get; set; }

    public string Title { get; set; }

    public ImageSource Icon { get; set; }

    public TimeSpan TimeToLive { get; set; }

    public bool Closeable { get; set; } = true;
}