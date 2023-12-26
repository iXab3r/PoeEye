using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Dialogs.Views;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using Unity;

namespace PoeShared.Dialogs.Services;

internal sealed class MessageBoxService : DisposableReactiveObjectWithLogger, IMessageBoxService
{
    private static readonly Binder<MessageBoxService> Binder = new();

    private static long dialogIdx;

    private readonly IApplicationAccessor applicationAccessor;
    private readonly IFactory<MessageBoxViewModel> genericMessageBoxFactory;
    private readonly IScheduler uiScheduler;
    private readonly SourceListEx<IMessageBoxViewModel> messageBoxes = new();
    private Stack<Window> windowStack = new();

    static MessageBoxService()
    {
        Binder.Bind(x => x.messageBoxes.Count > 0).To(x => x.IsOpen);
    }

    public MessageBoxService(
        IApplicationAccessor applicationAccessor,
        IFactory<MessageBoxViewModel> genericMessageBoxFactory, 
        [Dependency(WellKnownSchedulers.RedirectToUI)] IScheduler uiScheduler)
    {
        this.applicationAccessor = applicationAccessor;
        this.genericMessageBoxFactory = genericMessageBoxFactory;
        this.uiScheduler = uiScheduler;

        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsOpen { get; [UsedImplicitly] private set; }

    public async Task<T> ShowDialog<T>(IMessageBoxViewModel<T> content)
    {
        return await ShowMessageBox(content);
    }

    public async Task<bool> ShowConfirmation(string title, string content)
    {
        Log.Info($"Showing message box: { new { title, content }}");
        var result = await ShowInputBox(
            title,
            content,
            string.Empty,
            true,
            MessageBoxElement.Yes,
            MessageBoxElement.No
        );
        Log.Info($"Message box result: { new { title, result.DialogResult }}");
        return result.DialogResult == MessageBoxElement.Yes;
    }

    public async Task ShowMessage(string title, string content)
    {
        Log.Info($"Showing message box: { new { title, content }}");
        var result = await ShowInputBox(
            title,
            content,
            string.Empty,
            true,
            MessageBoxElement.Ok
        );
        Log.Info($"Message box result: { new { title, result.DialogResult }}");
    }

    public async Task<string> ShowInputBox(string title, string content, string contentHint)
    {
        var result = await ShowInputBox(
            title,
            content,
            string.Empty,
            true,
            MessageBoxElement.Ok,
            MessageBoxElement.Cancel
        );
        return result.DialogResult == MessageBoxElement.Ok ? result.InputContent : default;
    }

    public async Task<(MessageBoxElement DialogResult, string InputContent)> ShowInputBox(
        string title,
        string content,
        string contentHint,
        bool isReadOnly,
        params MessageBoxElement[] buttons
    )
    {
        Log.Debug($"Showing message box {new {title, content, contentHint, isReadOnly, buttons}}");

        var textContent = new MessageBoxTextContent()
        {
            Hint = contentHint,
            Text = content,
            IsReadOnly = isReadOnly
        };
        var result = await ShowDialog(title, textContent, buttons);
        return (result, textContent.Text);
    }

    public async Task<MessageBoxElement> ShowDialog(string title, object content, params MessageBoxElement[] buttons)
    {
        using var newMessageBox = genericMessageBoxFactory.Create();
        newMessageBox.Content = content;
        newMessageBox.Title = title;
        newMessageBox.Buttons.AddRange(buttons);
        var result = await ShowDialog(newMessageBox);
        return result;
    }

    private async Task<T> ShowMessageBox<T>(IMessageBoxViewModel<T> messageBox)
    {
        if (!IsOnUiThread)
        {
            return await Observable.FromAsync(() => ShowMessageBox(messageBox)).SubscribeOn(uiScheduler);
        }
        
        using var windowAnchors = new CompositeDisposable();

        var dialogId = $"Dialog#{Interlocked.Increment(ref dialogIdx)}";
        var log = Log.WithSuffix(dialogId);
        log.Info($"Creating new window for content: {messageBox}");
        
        messageBoxes.Add(messageBox);
        Disposable.Create(() => messageBoxes.Remove(messageBox)).AddTo(windowAnchors);
        
        var windowContainer = new MessageBoxContainerViewModel(log)
        {
            Content = messageBox,
        };

        var mainWindow = applicationAccessor.MainWindow;
        var window = new MessageBoxWindow
        {
            Owner = windowStack.Count <= 0 ? mainWindow : windowStack.Peek(),
            DataContext = windowContainer,
            WindowStartupLocation = WindowStartupLocation.Manual
        };
        windowStack.Push(window);
        Disposable.Create(() => windowStack.Pop()).AddTo(windowAnchors);

        var ownerWindowRect = UnsafeNative.GetWindowRect(window.Owner.GetWindowHandle());
        
        var childSize = messageBox.DefaultSize.IsNotEmptyArea() ? messageBox.DefaultSize : messageBox.MinSize;
        var updatedBounds = childSize.CenterInsideBounds(ownerWindowRect);
        log.Debug($"Centering rect {childSize} inside parent {ownerWindowRect}, result: {updatedBounds}");
        messageBox.NativeBounds = updatedBounds;

        window.WhenLoaded()
            .Do(args => log.Debug($"Message box is loaded"))
            .SubscribeSafe(() =>
            {
                log.Debug($"Assigning overlay view {this} to view-model {messageBox}");
                messageBox.SetOverlayWindow(window.Controller);
            }, log.HandleUiException)
            .AddTo(windowAnchors);
        
        log.Info($"Created new window: {window}");

        Disposable.Create(() =>
        {
            try
            {
                log.Info($"Closing window {window}");
                window.Close();
                log.Info($"Closed window {window}");
            }
            catch (Exception e)
            {
                log.Warn($"Failed to close window {window}",e);
            }
        }).AddTo(windowAnchors);
        
        var dialogCloseController = new CloseController<T>(_ => window.Close());
        var capturingCloseController = new CapturingCloseController<T>();
        var closeController = new ForwardingCloseController<T>(capturingCloseController, dialogCloseController);
        messageBox.CloseController = closeController;
        
        log.Info("Showing window");
        window.WhenLoaded().Subscribe(() =>
        {
            // there is a problem with child windows being obstructed by parent window despite the fact that Owner is set
            window.Topmost = true;
            window.Topmost = false;
        }).AddTo(windowAnchors);
        var result = window.ShowDialog();
        log.Info($"Window was closed, result: {result}");
        return capturingCloseController.Result;
    }
}