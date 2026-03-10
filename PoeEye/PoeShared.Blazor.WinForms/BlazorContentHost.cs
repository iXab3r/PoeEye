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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Blazor.WinForms.Services;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PropertyBinder;
using PropertyChanged;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.WinForms;

public abstract class ReactiveUserControl : UserControl, IDisposableReactiveObject
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Anchors.Dispose();
            GC.SuppressFinalize(this);
        }

        base.Dispose(disposing);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CompositeDisposable Anchors { get; } = new();

    public void RaisePropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed partial class BlazorContentHost : ReactiveUserControl
{
    private static readonly GlobalIdProvider IdProvider = new();
    private static readonly Binder<BlazorContentHost> Binder = new();

    private readonly ISharedResourceLatch isBusyLatch;
    private readonly SerialDisposable activeContentAnchors;
    private readonly WebViewServiceProvider webViewServiceProvider;
    private readonly IScheduler uiScheduler;
    private readonly DateTimeOffset timestampCreated;
    private readonly string globalId = IdProvider.Next("BCH");

    private DateTimeOffset timestampUpdated;
    private Microsoft.Web.WebView2.WinForms.WebView2? currentWebView;

    static BlazorContentHost()
    {
        Binder.Bind(x => x.isBusyLatch.IsBusy).To(x => x.IsBusy);
        Binder.Bind(x => x.UnhandledException == null ? null : BlazorContentHostUtilities.FormatExceptionMessage(x.UnhandledException)).To(x => x.UnhandledExceptionMessage);
    }

    public BlazorContentHost()
    {
        Log = GetType().PrepareLogger().WithSuffix(globalId);
        Disposable.Create(() => Log.Debug("Blazor content host is being disposed")).AddTo(Anchors);

        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        activeContentAnchors = new SerialDisposable().AddTo(Anchors);
        webViewServiceProvider = new WebViewServiceProvider().AddTo(Anchors);
        uiScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext());

        WebView = new BlazorWebViewEx().AddTo(Anchors);
        WebView.BlazorWebViewInitializing += BlazorWebViewInitializing;
        WebView.BlazorWebViewInitialized += BlazorWebViewInitialized;
        WebView.Services = webViewServiceProvider;

        ReloadCommand = BlazorCommandWrapper.Create<object>(ReloadExecuted);
        OpenDevToolsCommand = BlazorCommandWrapper.Create(OpenDevTools);
        ZoomInCommand = BlazorCommandWrapper.Create(ZoomIn);
        ZoomOutCommand = BlazorCommandWrapper.Create(ZoomOut);
        ResetZoomCommand = BlazorCommandWrapper.Create(ResetZoom);

        InitializeComponent();
        Disposable.Create(() => components?.Dispose()).AddTo(Anchors);

        WebView.RootComponents.Add(new RootComponent("headOutlet", typeof(HeadOutlet), parameters: null));
        WebView.RootComponents.Add(new RootComponent("#app", typeof(BlazorContentPresenterWrapper), parameters: null));

        WebView.Dock = DockStyle.Fill;
        webViewPanel.Controls.Add(WebView);
        WebView.BringToFront();

        Disposable.Create(() => DetachCurrentWebView(currentWebView)).AddTo(Anchors);

        Observable.FromEventPattern(h => Load += h, h => Load -= h)
            .Take(1)
            .ObserveOn(uiScheduler)
            .Subscribe(_ =>
            {
                Log.Debug("BlazorContentHost has been loaded");
                RefreshWebViewAvailability();
            }, Log.HandleUiException)
            .AddTo(Anchors);

        Observable.Merge(
                this.WhenAnyValue(x => x.IsBusy).Select(_ => System.Reactive.Unit.Default),
                this.WhenAnyValue(x => x.IsWebViewInstalled).Select(_ => System.Reactive.Unit.Default),
                this.WhenAnyValue(x => x.UnhandledException).Select(_ => System.Reactive.Unit.Default))
            .ObserveOn(uiScheduler)
            .Subscribe(_ => ApplyState(), Log.HandleUiException)
            .AddTo(Anchors);

        Observable.FromEventPattern(recoverButton, nameof(recoverButton.Click))
            .ObserveOn(uiScheduler)
            .SubscribeAsync(async _ => await Reload(), Log.HandleUiException)
            .AddTo(Anchors);

        WebViewAccessor.Instance.WhenAnyValue(x => x.IsInstalled)
            .Subscribe(x =>
            {
                IsWebViewInstalled = x;
            }, Log.HandleUiException)
            .AddTo(Anchors);

        RefreshWebViewAvailability();
        timestampCreated = DateTimeOffset.Now;
        Log.Debug("BlazorContentHost has been created");

        var generatedIndexFileName = "index.g.html";
        var contentRoot = "wwwroot";
        var hostPage = Path.Combine(contentRoot, generatedIndexFileName);

        var unityContainerSource = this.WhenAnyValue(x => x.Container)
            .Select(x => x ?? UnityServiceCollection.Instance.BuildServiceProvider().GetService<IUnityContainer>());

        Observable.CombineLatest(
                this.WhenAnyValue(x => x.AdditionalFileProvider),
                this.WhenAnyValue(x => x.AdditionalFiles)
                    .Select(x => (IReadOnlyList<IFileInfo>) (x?.ToArray() ?? Array.Empty<IFileInfo>())),
                this.WhenAnyValue(x => x.Configurator),
                unityContainerSource,
                this.WhenAnyValue(x => x.IsWebViewInstalled),
                this.WhenAnyValue(x => x.ReloadVersion),
                (additionalFileProvider, additionalFiles, configurator, container, isWebViewInstalled, reloadVersion) => new ControlState(container, additionalFileProvider, additionalFiles, configurator, isWebViewInstalled, reloadVersion))
            .ObserveOn(uiScheduler)
            .SubscribeAsync(async state =>
            {
                if (IsInDesignMode())
                {
                    return;
                }

                using var rent = isBusyLatch.Rent();

                timestampUpdated = DateTimeOffset.Now;
                Log.Debug($"Reloading control, time taken(created->updated): {timestampUpdated - timestampCreated}");

                var contentAnchors = new CompositeDisposable().AssignTo(activeContentAnchors);
                Disposable.Create(() => Log.Debug("Content is being disposed")).AddTo(contentAnchors);

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (!state.IsWebViewInstalled)
                {
                    Log.Debug("Skipping reload cycle, WebView2 runtime is not installed");
                    WebView.HostPage = null;
                    return;
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

                Log.Debug("Notifying visitor that we've started initializing the view");
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
                        IndexFileSubpath = "_content/PoeShared.Blazor.WinForms/index.html",
                        GeneratedIndexFileName = generatedIndexFileName,
                        HostPage = hostPage,
                        Log = Log,
                        State = state,
                        RegisterHostServices = serviceCollection =>
                        {
                            serviceCollection.AddBlazorWebView();
                            serviceCollection.AddWindowsFormsBlazorWebView();
                            serviceCollection.AddBlazorWebViewDeveloperTools();
                            serviceCollection.AddTransient<IComponentActivator>(_ => new BlazorComponentActivator(webViewServiceProvider, state.ChildContainer!));
                            serviceCollection.AddTransient(typeof(BlazorContentPresenterWrapper), _ => CreateContentPresenter(contentAnchors));

                            serviceCollection.AddSingleton<IUnityContainer>(_ => state.ChildContainer!);
                            serviceCollection.AddSingleton<IWebViewAccessor>(_ => WebViewAccessor.Instance);
                            serviceCollection.AddSingleton<IBlazorContentHostAccessor>(_ => new BlazorContentHostAccessor(this));
                            serviceCollection.AddSingleton<IBlazorControlLocationTracker>(_ => new ControlLocationTracker(this).AddTo(contentAnchors));
                            serviceCollection.AddSingleton<ICoreWebView2Accessor>(_ => new CoreWebView2Accessor(() => WebView.WebView));
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
            }, Log.HandleUiException)
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
        ApplyState();
    }

    private IFluentLog Log { get; }

    public Type? ViewType { get; set; }

    public object? Content { get; set; }

    public IEnumerable<IFileInfo>? AdditionalFiles { get; set; }

    public IFileProvider? AdditionalFileProvider { get; set; }

    public IBlazorContentControlConfigurator? Configurator { get; set; }

    public bool EnableHotkeys { get; set; } = true;

    public new IUnityContainer? Container { get; set; }

    public int ReloadVersion { get; private set; }

    public bool IsBusy { get; [UsedImplicitly] private set; }

    public bool IsWebViewInstalled { get; private set; }

    public BlazorWebViewEx WebView { get; }

    [AlsoNotifyFor(nameof(UnhandledExceptionMessage))]
    public Exception? UnhandledException { get; private set; }

    public string? UnhandledExceptionMessage { get; [UsedImplicitly] private set; }

    public ICommandWrapper ReloadCommand { get; }

    public ICommandWrapper OpenDevToolsCommand { get; }

    public ICommandWrapper ZoomInCommand { get; }

    public ICommandWrapper ZoomOutCommand { get; }

    public ICommandWrapper ResetZoomCommand { get; }

    public async Task ZoomIn()
    {
        var webView = currentWebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.ZoomFactor += 0.1;
    }

    public async Task ZoomOut()
    {
        var webView = currentWebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.ZoomFactor -= 0.1;
    }

    public async Task ResetZoom()
    {
        var webView = currentWebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.ZoomFactor = 1;
    }

    public async Task OpenDevTools()
    {
        var webView = currentWebView;
        if (webView == null)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.OpenDevToolsWindow();
    }

    public async Task Reload()
    {
        RefreshWebViewAvailability();
        if (!IsWebViewInstalled)
        {
            return;
        }

        var webView = currentWebView;
        if (webView == null)
        {
            ReloadVersion++;
            return;
        }

        if (UnhandledException != null)
        {
            Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
            UnhandledException = null;
        }

        await webView.EnsureCoreWebView2Async();
        webView.Reload();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (EnableHotkeys && keyData == Keys.F5)
        {
            _ = Reload();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void RefreshWebViewAvailability()
    {
        WebViewAccessor.Instance.Refresh();
        IsWebViewInstalled = WebViewAccessor.Instance.IsInstalled;
    }

    private async Task ReloadExecuted(object arg)
    {
        if (arg is false)
        {
            return;
        }

        await Reload();
    }

    private void BlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
    {
        var appArguments = webViewServiceProvider.ServiceProvider.GetRequiredService<IAppArguments>();
        var userDataFolder = appArguments.TempDirectory;
        Directory.CreateDirectory(userDataFolder);
        e.UserDataFolder = userDataFolder;
    }

    private void BlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        if (ReferenceEquals(currentWebView, e.WebView))
        {
            return;
        }

        DetachCurrentWebView(currentWebView);
        currentWebView = e.WebView;
        currentWebView.CoreWebView2.ProcessFailed += CoreWebView2OnProcessFailed;
        ApplyState();
    }

    private void CoreWebView2OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        var exception = new InvalidOperationException($"WebView2 process failed: {e.ProcessFailedKind}");
        Log.Error("WinForms WebView2 process failed", exception);
        UnhandledException = exception;
    }

    private BlazorContentPresenterWrapper CreateContentPresenter(CompositeDisposable contentAnchors)
    {
        var log = Log.WithSuffix(nameof(BlazorContentPresenterWrapper));
        log.Debug("Creating a new wrapper");
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
            .Subscribe(_ =>
            {
                var timestampInitialized = DateTimeOffset.Now;
                log.Debug($"Component has initialized, total time (updated->initialized): {timestampInitialized - timestampUpdated}");
            })
            .AddTo(contentAnchors);

        contentPresenter.WhenAnyValue(x => x.IsComponentLoaded)
            .Where(x => x)
            .Subscribe(_ =>
            {
                var timestampLoaded = DateTimeOffset.Now;
                log.Debug($"Component has loaded, total time (updated->loaded): {timestampLoaded - timestampUpdated}");
            })
            .AddTo(contentAnchors);

        contentPresenter.WhenAnyValue(x => x.IsComponentRendered)
            .Where(x => x)
            .Subscribe(_ =>
            {
                var timestampRendered = DateTimeOffset.Now;
                log.Debug($"Component has rendered, total time (updated->rendered): {timestampRendered - timestampUpdated}");
            })
            .AddTo(contentAnchors);

        return contentPresenter;
    }

    private void DetachCurrentWebView(Microsoft.Web.WebView2.WinForms.WebView2? webView)
    {
        if (webView?.CoreWebView2 == null)
        {
            return;
        }

        webView.CoreWebView2.ProcessFailed -= CoreWebView2OnProcessFailed;
    }

    private void ApplyState()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyState));
            return;
        }

        progressBar.Visible = IsBusy;
        recoverButton.Enabled = !IsBusy;

        var hasShellError = UnhandledException != null;
        webViewPanel.Visible = IsWebViewInstalled && !hasShellError;
        errorPanel.Visible = IsWebViewInstalled && hasShellError;
        unavailablePanel.Visible = !IsWebViewInstalled;

        if (!IsWebViewInstalled)
        {
            errorTitleLabel.Text = string.Empty;
            errorDetailsTextBox.Text = string.Empty;
            return;
        }

        if (hasShellError)
        {
            errorTitleLabel.Text = "Render exception";
            errorDetailsTextBox.Text = BlazorContentHostUtilities.FormatExceptionMessage(UnhandledException!);
            recoverButton.Text = "Try to recover";
            return;
        }

        errorTitleLabel.Text = string.Empty;
        errorDetailsTextBox.Text = string.Empty;
        recoverButton.Text = "Try to recover";
    }

    private bool IsInDesignMode()
    {
        return LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
    }

    private sealed class ControlState : DisposableReactiveObject
    {
        public ControlState(IUnityContainer? container, IFileProvider? additionalFileProvider, IReadOnlyList<IFileInfo> additionalFiles, IBlazorContentControlConfigurator? configurator, bool isWebViewInstalled, int reloadVersion)
        {
            Container = container;
            ChildContainer = container?.CreateChildContainer().AddTo(Anchors);
            AdditionalFileProvider = additionalFileProvider;
            AdditionalFiles = additionalFiles;
            Configurator = configurator;
            IsWebViewInstalled = isWebViewInstalled;
            ReloadVersion = reloadVersion;
        }

        public IUnityContainer? Container { get; }

        public IUnityContainer? ChildContainer { get; }

        public IFileProvider? AdditionalFileProvider { get; }

        public IReadOnlyList<IFileInfo> AdditionalFiles { get; }

        public IBlazorContentControlConfigurator? Configurator { get; }

        public bool IsWebViewInstalled { get; }

        public int ReloadVersion { get; }
    }
}
