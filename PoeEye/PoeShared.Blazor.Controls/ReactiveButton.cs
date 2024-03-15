using System.Reactive.Linq;
using AntDesign;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using System;

namespace PoeShared.Blazor.Controls;

public partial class ReactiveButton
{
    private static readonly Binder<ReactiveButton> Binder = new();
    private readonly ISharedResourceLatch isBusyLatch;

    static ReactiveButton()
    {
        Binder.Bind(x => x.isBusyLatch.IsBusy || (x.CommandEx != null && x.CommandEx.IsBusy)).To<bool, ReactiveButton, bool>(x => x.IsBusy);
    }

    public ReactiveButton()
    {
        this.isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        Class = "btn btn-outline-secondary";
        
        this.WhenAnyValue<ReactiveButton, ICommandWrapper>(x => x.CommandEx)
            .Select(command => command != null ? command.WhenAnyValue(x => x.Error) : Observable.Return<string>(null))
            .Switch()
            .Subscribe(x => Error = x, Log.HandleUiException)
            .AddTo(Anchors);

        this.WhenAnyValue<ReactiveButton, string>(x => x.Error)
            .Skip(1) // ignore the error which is already there
            .Where(x => !string.IsNullOrEmpty(x))
            .SubscribeAsync(async error =>
            {
                if (notificationService == null)
                {
                    return;
                }
                
                Log.Warn($"Showing error notification, message: {error}");
                notificationService.Error(new NotificationConfig()
                {
                    Message = error,
                    NotificationType = NotificationType.Error,
                    Duration = 30,
                    Placement = NotificationPlacement.TopRight
                }).AndForget(ignoreExceptions: true);
                
            }, Log.HandleUiException)
            .AddTo(Anchors);
        
        Binder.Attach(this).AddTo(Anchors);
    }
}