using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Blazor specialization of <see cref="NativeWindow"/> - hosts Blazor/WebView2 content (body + title bar)
/// on top of the shared thread-safe windowing core (dispatcher, command channel, property mirror).
/// Things to note:
/// - if Application is Shutting down, windows WILL NOT be created, this is in Window code. Need to track it.
/// </summary>
internal partial class BlazorWindow : NativeWindow, IWpfBlazorWindow
{
    private readonly IUnityContainer unityContainer;
    private readonly ReactiveCompositeFileProvider compositeFileProvider;
    private readonly SerialDisposable additionalFileProviderAnchor;

    public BlazorWindow(
        IUnityContainer unityContainer,
        [OptionalDependency] IBlazorWindowConfigurator windowConfigurator = null,
        [OptionalDependency] Dispatcher dispatcher = null) : base("BWnd", dispatcher)
    {
        this.unityContainer = unityContainer;
        compositeFileProvider = new ReactiveCompositeFileProvider().AddTo(Anchors);
        additionalFileProviderAnchor = new SerialDisposable().AddTo(Anchors);

#pragma warning disable CS0618 // Type or member is obsolete
        this.RaiseWhenSourceValue(x => x.ViewDataContext, this, x => x.DataContext).AddTo(Anchors);
#pragma warning restore CS0618 // Type or member is obsolete

        Disposable.Create(() =>
        {
            Log.Debug("Blazor window is disposed, clenaing up references");
            ViewType = null;
            ViewTypeForTitleBar = null;
            Container = null;
            DataContext = null;
            ControlConfigurator = null;
        }).AddTo(Anchors);

        if (windowConfigurator != null)
        {
            var sw = ValueStopwatch.StartNew();
            var configuratorType = windowConfigurator.GetType();
            Log.Debug($"Invoking {nameof(IBlazorWindowConfigurator)} {configuratorType.FullName}");
            try
            {
                windowConfigurator.Configure(this);
            }
            finally
            {
                Log.Debug($"{nameof(IBlazorWindowConfigurator)} {configuratorType.FullName} completed in {sw.ElapsedMilliseconds}ms");
            }
        }
    }

    public Type ViewType { get; set; }

    public Type ViewTypeForTitleBar { get; set; }

    public object DataContext { get; set; }

    [Obsolete($"Replaced with {nameof(DataContext)} - to be removed in future versions")]
    [Browsable(false)]
    public object ViewDataContext
    {
        get => DataContext;
        set => DataContext = value;
    }

    public IUnityContainer Container { get; set; }

    public IBlazorContentControlConfigurator ControlConfigurator { get; set; }

    public ImmutableArray<IFileInfo> AdditionalFiles { get; set; } = ImmutableArray<IFileInfo>.Empty;

    public IFileProvider AdditionalFileProvider { get; set; }

    /// <summary>
    /// Blazor windows host Blazor content - arbitrary WPF content is not supported, use <see cref="ViewType"/> instead.
    /// </summary>
    public override Func<INativeWindow, UIElement> ContentFactory
    {
        get => null;
        set
        {
            if (value != null)
            {
                throw new NotSupportedException($"{nameof(BlazorWindow)} hosts Blazor content - assign {nameof(ViewType)} instead of {nameof(ContentFactory)}");
            }
        }
    }

    public IDisposable RegisterFileProvider(IFileProvider fileProvider)
    {
        return compositeFileProvider.Add(fileProvider);
    }

    public void ShowDevTools()
    {
        Log.Debug("Enqueueing ShowDevTools command");
        EnqueueUpdate(new ShowDevToolsCommand());
    }

    private protected override WindowView CreateWindowView()
    {
        return new BlazorWindowView(this);
    }

    private protected override void HandleInitialState(WindowView window)
    {
        base.HandleInitialState(window);
        HandleEvent(new SetBlazorControlConfigurator(ControlConfigurator));
        HandleEvent(new SetBlazorAdditionalFiles(AdditionalFiles));
        HandleEvent(new SetBlazorFileProvider(AdditionalFileProvider));
        HandleEvent(new SetBlazorUnityContainer(Container));
    }

    private protected override void SubscribeToContentEvents(WindowView window, IObserver<IWindowEvent> observer, CompositeDisposable anchors)
    {
        var view = (BlazorWindowView) window;

        // blazor-related events
        this.WhenAnyValue(x => x.Container)
            .Skip(1)
            .Subscribe(x => observer.OnNext(new SetBlazorUnityContainer(x)))
            .AddTo(anchors);

        this.WhenAnyValue(x => x.AdditionalFileProvider)
            .Skip(1)
            .Subscribe(x => observer.OnNext(new SetBlazorFileProvider(x)))
            .AddTo(anchors);

        this.WhenAnyValue(x => x.AdditionalFiles)
            .Skip(1)
            .Subscribe(x => observer.OnNext(new SetBlazorAdditionalFiles(x)))
            .AddTo(anchors);

        this.WhenAnyValue(x => x.ControlConfigurator)
            .Skip(1)
            .Subscribe(x => observer.OnNext(new SetBlazorControlConfigurator(x)))
            .AddTo(anchors);

        // events propagation - WebView2 swallows input, so events are tracked on the WebViews rather than on the window
        SubscribeToInputEvents(view.BodyContentControl.WebView, anchors);
        SubscribeToInputEvents(view.TitleBarContentControl.WebView, anchors);
    }

    private protected override bool HandleCommand(IWindowEvent windowEvent, WindowView window)
    {
        var view = (BlazorWindowView) window;
        switch (windowEvent)
        {
            case ShowDevToolsCommand:
            {
                Log.Debug($"Showing dev tools");
                view.ContentControl.OpenDevTools().AndForget();
                return true;
            }
            case SetBlazorUnityContainer command:
            {
                Log.Debug($"Updating {nameof(Container)} to {command.ChildContainer}");
                view.Container = command.ChildContainer;
                return true;
            }
            case SetBlazorFileProvider command:
            {
                Log.Debug($"Updating {nameof(AdditionalFileProvider)} to {command.FileProvider}");
                additionalFileProviderAnchor.Disposable = command.FileProvider == null
                    ? null
                    : compositeFileProvider.Add(command.FileProvider);
                return true;
            }
            case SetBlazorAdditionalFiles command:
            {
                Log.Debug($"Updating {nameof(AdditionalFiles)} to {command.AdditionalFiles}");
                view.BodyContentControl.AdditionalFiles = command.AdditionalFiles;
                view.TitleBarContentControl.AdditionalFiles = command.AdditionalFiles;
                return true;
            }
            case SetBlazorControlConfigurator command:
            {
                Log.Debug($"Updating {nameof(ControlConfigurator)} to {command.ControlConfigurator}");
                view.BodyContentControl.Configurator = command.ControlConfigurator;
                view.TitleBarContentControl.Configurator = command.ControlConfigurator;
                return true;
            }
            default:
                return base.HandleCommand(windowEvent, window);
        }
    }

    private sealed record ShowDevToolsCommand : IWindowCommand;

    private sealed record SetBlazorUnityContainer(IUnityContainer ChildContainer) : IWindowCommand;

    private sealed record SetBlazorFileProvider(IFileProvider FileProvider) : IWindowCommand;

    private sealed record SetBlazorControlConfigurator(IBlazorContentControlConfigurator ControlConfigurator) : IWindowCommand;

    private sealed record SetBlazorAdditionalFiles(ImmutableArray<IFileInfo> AdditionalFiles) : IWindowCommand;
}
