#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PropertyBinder;
using ReactiveUI;
using Unity;
using Unity.Microsoft.DependencyInjection;
using ServiceProvider = Unity.Microsoft.DependencyInjection.ServiceProvider;

namespace PoeShared.Blazor.Wpf;

public class BlazorContentControl : Control, IBlazorContentControl
{
    private static readonly GlobalIdProvider IdProvider = new();
    private static readonly Binder<BlazorContentControl> Binder = new();

    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

    public static readonly DependencyProperty AdditionalFilesProperty = DependencyProperty.Register(
        nameof(AdditionalFiles), typeof(IEnumerable<IFileInfo>), typeof(BlazorContentControl), new PropertyMetadata(default(IEnumerable<IFileInfo>)));

    public static readonly DependencyProperty AdditionalFileProviderProperty = DependencyProperty.Register(
        nameof(AdditionalFileProvider), typeof(IFileProvider), typeof(BlazorContentControl), new PropertyMetadata(default(IFileProvider)));

    public static readonly DependencyProperty ConfiguratorProperty = DependencyProperty.Register(
        nameof(Configurator), typeof(IBlazorContentControlConfigurator), typeof(BlazorContentControl), new PropertyMetadata(default(IBlazorContentControlConfigurator)));

    public static readonly DependencyProperty EnableHotkeysProperty = DependencyProperty.Register(
        nameof(EnableHotkeys), typeof(bool), typeof(BlazorContentControl), new PropertyMetadata(true));

    public static readonly DependencyProperty ContainerProperty = DependencyProperty.Register(
        nameof(Container), typeof(IUnityContainer), typeof(BlazorContentControl), new PropertyMetadata(default(IUnityContainer)));

    private readonly ISharedResourceLatch isBusyLatch;
    private readonly SerialDisposable activeContentAnchors;
    private readonly SerialDisposable activeViewAnchors;
    private readonly WebViewServiceProvider webViewServiceProvider;
    private readonly JSComponentConfigurationStoreAccessor jsComponentConfigurationStoreAccessor;
    private readonly DispatcherScheduler uiScheduler;
    private readonly DateTimeOffset timestampCreated;
    private readonly string globalId = IdProvider.Next("BCC");

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
        Binder.Bind(x => x.isBusyLatch.IsBusy).To(x => x.IsBusy);
        Binder.Bind(x => x.UnhandledException == null ? null : BlazorContentHostUtilities.FormatExceptionMessage(x.UnhandledException)).To(x => x.UnhandledExceptionMessage);
    }

    public BlazorContentControl()
    {
        Log = this.GetType().PrepareLogger().WithSuffix(globalId);
        Disposable.Create(() => Log.Debug("Blazor content control is being disposed")).AddTo(Anchors);
        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        activeContentAnchors = new SerialDisposable().AddTo(Anchors);
        activeViewAnchors = new SerialDisposable().AddTo(Anchors);
        webViewServiceProvider = new WebViewServiceProvider().AddTo(Anchors);
        uiScheduler = DispatcherScheduler.Current;

        WebView = new BlazorWebViewEx().AddTo(Anchors);
        WebView.UnhandledException += OnUnhandledException;
        WebView.BlazorWebViewInitializing += BlazorWebViewInitializing;

        /*
        Assigning Services will trigger initialization of WebView.
        That means that most registrations should be done at this point.
        */
        WebView.Services = webViewServiceProvider;

        ReloadCommand = BlazorCommandWrapper.Create<object>(ReloadExecuted);
        OpenDevToolsCommand = BlazorCommandWrapper.Create(OpenDevTools);
        ZoomInCommand = BlazorCommandWrapper.Create(ZoomIn);
        ZoomOutCommand = BlazorCommandWrapper.Create(ZoomOut);
        ResetZoomCommand = BlazorCommandWrapper.Create(ResetZoom);

        this.Loaded += OnLoaded;
        this.Initialized += OnInitialized;
        Log.Debug($"BlazorContentControl has been created");
        timestampCreated = DateTimeOffset.Now;

        new RootComponent
        {
            Selector = "headOutlet",
            ComponentType = typeof(HeadOutlet)
        }.AddTo(WebView.RootComponents);

        new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(BlazorContentPresenterWrapper)
        }.AddTo(WebView.RootComponents);

        var generatedIndexFileName = "index.g.html";
        var contentRoot = "wwwroot";
        var hostPage = Path.Combine(contentRoot, generatedIndexFileName); // wwwroot must be included as a part of path to become ContentRoot;

        var unityContainerSource = this.WhenAnyValue(x => x.Container)
            .Select(x => x ?? UnityServiceCollection.Instance.BuildServiceProvider().GetService<IUnityContainer>());

        Observable.CombineLatest(
                this.WhenAnyValue(x => x.AdditionalFileProvider),
                this.WhenAnyValue(x => x.AdditionalFiles)
                    .Select(x => (IReadOnlyList<IFileInfo>) (x?.ToArray() ?? Array.Empty<IFileInfo>())),
                this.WhenAnyValue(x => x.Configurator),
                unityContainerSource,
                (additionalFileProvider, additionalFiles, visitor, container) => new ControlState(container, additionalFileProvider, additionalFiles, visitor))
            .ObserveOnIfNeeded(uiScheduler)
            .SubscribeAsync(async state =>
            {
                using var rent = isBusyLatch.Rent();

                var timestampUpdated = DateTimeOffset.Now;
                Log.Debug($"Reloading control, time taken(created->updated): {timestampUpdated - timestampCreated}");

                var contentAnchors = new CompositeDisposable().AssignTo(activeContentAnchors);
                Disposable.Create(() => Log.Debug("Content is being disposed")).AddTo(contentAnchors);

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (state.Container == null || state.ChildContainer == null)
                {
                    Log.Debug("Skipping reload cycle, containers are not initialized");
                    return;
                }

                var configurator = new CompositeBlazorContentControlConfigurator();
                configurator.Add(new LoggingBlazorContentControlConfigurator(Log));
                if (state.Configurator != null)
                {
                    configurator.Add(state.Configurator);
                }

                Log.Debug($"Notifying visitor that we've started initializing the view");
                await configurator.OnConfiguringAsync();

                try
                {
                    await BlazorContentHostCompositor.ComposeAsync(new BlazorContentHostCompositionContext
                    {
                        ChildContainer = state.ChildContainer!,
                        Configurator = configurator,
                        WebViewServiceProvider = webViewServiceProvider,
                        RootComponents = WebView.RootComponents,
                        RootComponentsStore = WebView.RootComponents.JSComponents,
                        AdditionalFileProvider = state.AdditionalFileProvider,
                        AdditionalFiles = state.AdditionalFiles,
                        IndexFileSubpath = "_content/PoeShared.Blazor.Wpf/index.html",
                        GeneratedIndexFileName = generatedIndexFileName,
                        HostPage = hostPage,
                        Log = Log,
                        State = state,
                        RegisterHostServices = serviceCollection =>
                        {
                            serviceCollection.AddBlazorWebView();
                            serviceCollection.AddWpfBlazorWebView();
                            serviceCollection.AddBlazorWebViewDeveloperTools();

                            // this is needed mostly for compatibility-reasons with views that use UnityContainer
                            serviceCollection.AddTransient<IComponentActivator>(_ => new BlazorComponentActivator(webViewServiceProvider, state.ChildContainer!));

                            // views have to be transient to allow to re-create them if needed (e.g. on error)
                            serviceCollection.AddTransient(typeof(BlazorContentPresenterWrapper), _ => CreateContentPresenter(contentAnchors, timestampUpdated));

                            serviceCollection.AddSingleton<IUnityContainer>(_ => state.ChildContainer!);
                            serviceCollection.AddSingleton<IBlazorControlLocationTracker>(_ => new FrameworkElementLocationTracker(this).AddTo(contentAnchors));
                            serviceCollection.AddSingleton<IBlazorContentControlAccessor>(_ => new BlazorContentControlAccessor(this));
                            serviceCollection.AddSingleton<ICoreWebView2Accessor>(_ => new CoreWebView2Accessor(WebView.WebView));
                        },
                        SetWebViewFileProvider = fileProvider => WebView.FileProvider = fileProvider,
                        GetCurrentHostPage = () => WebView.HostPage,
                        IsWebViewReady = () => WebView.WebView?.CoreWebView2 != null,
                        ReloadCurrentPage = Reload,
                        SetHostPage = value => WebView.HostPage = value
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to initialize view using {state}", ex);
                    UnhandledException = ex;
                }
                finally
                {
                    Disposable.Create(() => Log.Debug("Content has been disposed")).AddTo(contentAnchors);
                }
            })
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
    }

    protected IFluentLog Log { get; }

    private void OnInitialized(object sender, EventArgs e)
    {
        Log.Debug($"BlazorContentControl has been initialized");
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Log.Debug($"BlazorContentControl has been loaded");
    }

    private BlazorContentPresenterWrapper CreateContentPresenter(CompositeDisposable contentAnchors, DateTimeOffset timestampUpdated)
    {
        var log = Log.WithSuffix(nameof(BlazorContentPresenterWrapper));
        log.Debug($"Creating a new wrapper");
        var contentPresenter = new BlazorContentPresenterWrapper().AddTo(contentAnchors);

        this.WhenAnyValue(x => x.ViewType)
            .Subscribe(x =>
            {
                log.Debug($"Updating view type: {x}");
                contentPresenter.ViewType = x;
            })
            .AddTo(contentAnchors);

        this.WhenAnyValue(x => x.Content)
            .Subscribe(content =>
            {
                log.Debug($"Updating content to {content}");
                contentPresenter.Content = content;
            })
            .AddTo(contentAnchors);

        contentPresenter.WhenAnyValue(x => x.IsComponentInitialized)
            .Where(x => x)
            .Subscribe(() =>
            {
                var timestampInitialized = DateTimeOffset.Now;
                log.Debug($"Component has initialized, total time (updated->initialized): {timestampInitialized - timestampUpdated}");
            })
            .AddTo(contentAnchors);
        contentPresenter.WhenAnyValue(x => x.IsComponentLoaded)
            .Where(x => x)
            .Subscribe(() =>
            {
                var timestampLoaded = DateTimeOffset.Now;
                log.Debug($"Component has loaded, total time (updated->loaded): {timestampLoaded - timestampUpdated}");
            })
            .AddTo(contentAnchors);
        contentPresenter.WhenAnyValue(x => x.IsComponentRendered)
            .Where(x => x)
            .Subscribe(() =>
            {
                var timestampRendered = DateTimeOffset.Now;
                log.Debug($"Component has rendered, total time (updated->rendered): {timestampRendered - timestampUpdated}");
            })
            .AddTo(contentAnchors);

        return contentPresenter;
    }

    public Type ViewType
    {
        get => (Type) GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    public object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public IUnityContainer Container
    {
        get => (IUnityContainer) GetValue(ContainerProperty);
        set => SetValue(ContainerProperty, value);
    }

    public IEnumerable<IFileInfo> AdditionalFiles
    {
        get => (IEnumerable<IFileInfo>) GetValue(AdditionalFilesProperty);
        set => SetValue(AdditionalFilesProperty, value);
    }

    public IFileProvider AdditionalFileProvider
    {
        get => (IFileProvider) GetValue(AdditionalFileProviderProperty);
        set => SetValue(AdditionalFileProviderProperty, value);
    }

    public IBlazorContentControlConfigurator Configurator
    {
        get => (IBlazorContentControlConfigurator) GetValue(ConfiguratorProperty);
        set => SetValue(ConfiguratorProperty, value);
    }

    public bool EnableHotkeys
    {
        get => (bool) GetValue(EnableHotkeysProperty);
        set => SetValue(EnableHotkeysProperty, value);
    }

    public bool IsBusy { get; [UsedImplicitly] private set; }

    /// <summary>
    ///     We have to dynamically recreate WebView when needed as it is EXTREMELY unfriendly for any changes of associated
    ///     properties
    /// </summary>
    public BlazorWebViewEx WebView { get; }

    public Exception? UnhandledException { get; private set; }

    public string UnhandledExceptionMessage { get; [UsedImplicitly] private set; }

    public ICommandWrapper ReloadCommand { get; }

    public ICommandWrapper OpenDevToolsCommand { get; }

    public ICommandWrapper ZoomInCommand { get; }

    public ICommandWrapper ZoomOutCommand { get; }

    public ICommandWrapper ResetZoomCommand { get; }

    public async Task ZoomIn()
    {
        var webView = WebView?.WebView;
        if (webView == null)
        {
            return;
        }

        webView.ZoomFactor += 0.1;
    }

    public async Task ZoomOut()
    {
        var webView = WebView?.WebView;
        if (webView == null)
        {
            return;
        }

        webView.ZoomFactor -= 0.1;
    }

    public async Task ResetZoom()
    {
        var webView = WebView?.WebView;
        if (webView == null)
        {
            return;
        }

        webView.ZoomFactor = 1;
    }

    public async Task OpenDevTools()
    {
        var webView = WebView?.WebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.OpenDevToolsWindow();
    }

    public async Task Reload()
    {
        if (UnhandledException != null)
        {
            Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
            UnhandledException = null;
        }

        var webView = WebView?.WebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.Reload();
    }

    private async Task ReloadExecuted(object arg)
    {
        if (arg is false)
        {
            return;
        }

        await Reload();
    }

    private void BlazorWebViewInitializing(object sender, BlazorWebViewInitializingEventArgs e)
    {
        var appArguments = webViewServiceProvider.ServiceProvider.GetRequiredService<IAppArguments>();
        e.UserDataFolder = appArguments.TempDirectory;
    }
    
    private void OnUnhandledException(object sender, WpfDispatcherUnhandlerExceptionEventArgs e)
    {
        switch (sender)
        {
            case BlazorWebView webView:
                webView.UnhandledException -= OnUnhandledException; //after webview crash it will be recreated anyways
                Log.Error(ReferenceEquals(sender, WebView.WebView) ? $"WebView has crashed: {sender}" : $"Obsolete(replaced) WebView has crashed: {sender}", e.Exception);
                break;
            case Dispatcher _:
                Log.Error($"WebView dispatcher has encountered an error", e.Exception);
                break;
            default:
                Log.Error($"WebView has encountered an error from {sender}", e.Exception);
                break;
        }

        UnhandledException = e.Exception;
        e.Handled = true; // JS context is already dead at this point
    }

    public void Dispose()
    {
        Anchors.Dispose();
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        RaisePropertyChanged(e.Property.Name);
    }

    private sealed class ControlState : DisposableReactiveObject
    {
        public IUnityContainer? Container { get; }
        public IUnityContainer? ChildContainer { get; }
        public IFileProvider? AdditionalFileProvider { get; }
        public IReadOnlyList<IFileInfo> AdditionalFiles { get; }
        public IBlazorContentControlConfigurator? Configurator { get; }
        
        public ControlState(IUnityContainer? container, IFileProvider? additionalFileProvider, IReadOnlyList<IFileInfo> additionalFiles, IBlazorContentControlConfigurator? configurator)
        {
            Container = container;
            ChildContainer = container?.CreateChildContainer().AddTo(Anchors);
            AdditionalFileProvider = additionalFileProvider;
            AdditionalFiles = additionalFiles;
            Configurator = configurator;
            
        }
    }
}
