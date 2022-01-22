using System;
using System.Windows.Media;
using PoeShared.Scaffolding;

namespace PoeShared.Notifications.ViewModels;

public interface INotificationViewModel : IDisposableReactiveObject, ICloseable
{
    string Title { get; set; }
        
    ImageSource Icon { get; set; }
        
    TimeSpan TimeToLive { get; set; }
        
    bool Closeable { get; }
        
    bool Interactive { get; }
}