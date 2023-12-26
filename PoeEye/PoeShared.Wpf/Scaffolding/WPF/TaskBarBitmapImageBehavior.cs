using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interactivity;
using H.NotifyIcon;
using PoeShared.Logging;
using Polly;

namespace PoeShared.Scaffolding.WPF;

public sealed class TaskBarBitmapImageBehavior : Behavior<TaskbarIcon>
{
    private static readonly IFluentLog Log = typeof(TaskBarBitmapImageBehavior).PrepareLogger();

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        "Icon", typeof(Icon), typeof(TaskBarBitmapImageBehavior), new PropertyMetadata(default(Icon)));

    private static readonly int RetryCount = 10;
    private static readonly TimeSpan RetryTimeout = TimeSpan.FromMilliseconds(10);

    private readonly SerialDisposable attachmentAnchors = new SerialDisposable();

    public Icon Icon
    {
        get => (Icon) GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        attachmentAnchors.Disposable =
            this.Observe(IconProperty)
                .Select(_ => Icon)
                .SubscribeSafe(HandleImageChange, Log.HandleUiException);
    }

    private void HandleImageChange(Icon source)
    {
        // There is a problem with random exception which happens(very rarely) on some machines. 
        // Probably somethings related to simultaneous updates/taskbar redraw yada-yada
        // Exception: System.InvalidOperationException: UpdateIcon failed. at void H.NotifyIcon.Core.TrayIcon.UpdateIcon(IntPtr handle) at void H.NotifyIcon.TaskbarIcon.IconPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        var result = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                RetryCount,
                retryAttempt => RetryTimeout,
                (exception, timeSpan, context) => { Log.Warn($"Failed to update taskbar icon", exception); }
            ).ExecuteAndCapture(context =>
            {
                Log.Info("Assigning new taskbar icon");
                AssociatedObject.Icon = source;
                Log.Info("Assigned new taskbar icon");
            }, new Context());
        
        if (result.Outcome != OutcomeType.Successful)
        {
            Log.Warn("Could not update taskbar icon after multiple attempts, final exception:", result.FinalException);
        }
    }

    protected override void OnDetaching()
    {
        attachmentAnchors.Disposable = null;
        base.OnDetaching();
    }
}