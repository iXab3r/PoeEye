using System;
using PoeShared.Notifications.ViewModels;

namespace PoeShared.Notifications.Services;

public interface INotificationsService
{
    void CloseAll();
        
    IDisposable AddNotification(INotificationViewModel notification);
}