using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using PoeShared.Blazor.Prism;
using PoeShared.Blazor.Scaffolding;
using PoeShared.Blazor.Wpf.Scaffolding;
using PoeShared.Blazor.Wpf.Services;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

public class BlazorContentControl : ReactiveControl, IBlazorContentControl
{
    private static readonly Binder<BlazorContentControl> Binder = new();
    private static readonly IFluentLog Log = typeof(BlazorContentControl).PrepareLogger();

    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

    public static readonly DependencyProperty AdditionalFilesProperty = DependencyProperty.Register(
        nameof(AdditionalFiles), typeof(IEnumerable<IFileInfo>), typeof(BlazorContentControl), new PropertyMetadata(default(IEnumerable<IFileInfo>)));

    public static readonly DependencyProperty EnableHotkeysProperty = DependencyProperty.Register(
        nameof(EnableHotkeys), typeof(bool), typeof(BlazorContentControl), new PropertyMetadata(true));

    public static readonly DependencyProperty ContainerProperty = DependencyProperty.Register(
        nameof(Container), typeof(IUnityContainer), typeof(BlazorContentControl), new PropertyMetadata(default(IUnityContainer)));
    
    private readonly ISharedResourceLatch isBusyLatch;
    private readonly SerialDisposable activeContentAnchors;
    private readonly SerialDisposable activeViewAnchors;
    private readonly WebViewServiceProvider webViewServiceProvider;
    private readonly JSComponentConfigurationStoreAccessor jsComponentConfigurationStoreAccessor;

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
        Binder.Bind(x => x.isBusyLatch.IsBusy).To(x => x.IsBusy);
        Binder.Bind(x => x.UnhandledException == null ? null : FormatExceptionMessage(x.UnhandledException)).To(x => x.UnhandledExceptionMessage);
    }

    public BlazorContentControl()
    {
        Disposable.Create(() => Log.Debug("Blazor content control is being disposed")).AddTo(Anchors);
        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        activeContentAnchors = new SerialDisposable().AddTo(Anchors);
        activeViewAnchors = new SerialDisposable().AddTo(Anchors);
        webViewServiceProvider = new WebViewServiceProvider().AddTo(Anchors);

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

        var serviceCollection = new ServiceCollection
        {
            UnityServiceCollection.Instance
        };
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddWpfBlazorWebView();
        serviceCollection.AddBlazorWebViewDeveloperTools();

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
        
        var indexFileContentTemplate = ResourceReader.ReadResourceAsString(Assembly.GetExecutingAssembly(), @"wwwroot.index.html");
        var generatedIndexFileName = "index.g.html";
        var contentRoot = "wwwroot";
        var hostPage = Path.Combine(contentRoot, generatedIndexFileName); // wwwroot must be included as a part of path to become ContentRoot;

        var unityContainerSource = this.WhenAnyValue(x => x.Container)
            .Select(x => x ?? UnityServiceCollection.Instance.BuildServiceProvider().GetService<IUnityContainer>());

        this.WhenAnyValue(x => x.ViewType)
            .CombineLatest(unityContainerSource, (viewType, container) => new {viewType, container})
            .ObserveOnDispatcher()
            .SubscribeAsync(async state =>
            {
                using var rent = isBusyLatch.Rent();

                Log.Debug($"Reloading control, new content type: {state.viewType}");

                var contentAnchors = new CompositeDisposable().AssignTo(activeContentAnchors);
                Disposable.Create(() => Log.Debug("Content is being disposed")).AddTo(contentAnchors);

                WebView.FileProvider.FilesByName.Clear();

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                try
                {
                    var childServiceCollection = new ServiceCollection {serviceCollection};

                    // this is needed mostly for compatibility-reasons with views that use UnityContainer
                    childServiceCollection.AddTransient<IComponentActivator>(_ => new BlazorComponentActivator(webViewServiceProvider, state.container));

                    // views have to be transient to allow to re-create them if needed (e.g. on error)
                    childServiceCollection.AddTransient(typeof(BlazorContentPresenterWrapper), _ =>
                    {
                        var contentPresenter = new BlazorContentPresenterWrapper()
                        {
                            ViewType = state.viewType,
                        }.AddTo(contentAnchors);

                        this.WhenAnyValue(contentControl => contentControl.Content)
                            .Subscribe(content => { contentPresenter.Content = content; })
                            .AddTo(contentAnchors);
                        return contentPresenter;
                    });

                    childServiceCollection.AddSingleton<IBlazorControlLocationTracker>(sp => new FrameworkElementLocationTracker(this).AddTo(contentAnchors));
                    childServiceCollection.AddSingleton<IBlazorContentControlAccessor>(sp => new BlazorContentControlAccessor(this));
                    childServiceCollection.AddSingleton<IServiceScopeFactory>(sp => new UnityFallbackServiceScopeFactory(sp, state.container));

                    var unityServiceDescriptors = state.container.ToServiceDescriptors();
                    childServiceCollection.Add(unityServiceDescriptors);

                    var childServiceProvider = childServiceCollection.BuildServiceProvider();

                    var clock = childServiceProvider.GetRequiredService<IClock>();
                    var scoped = childServiceProvider.CreateScope();
                    var scopedClock = scoped.ServiceProvider.GetRequiredService<IClock>();

                    webViewServiceProvider.ServiceProvider = childServiceProvider;

                    var blazorContentRepository = childServiceProvider.GetRequiredService<IBlazorContentRepository>();
                    var repositoryAdditionalFiles = blazorContentRepository.AdditionalFiles.Items.ToArray();
                    var controlAdditionalFiles = AdditionalFiles?.ToArray() ?? Array.Empty<IFileInfo>();
                    var additionalFiles = repositoryAdditionalFiles.Concat(controlAdditionalFiles).ToArray();
                    if (additionalFiles.Any())
                    {
                        Log.Debug($"Loading additional files({additionalFiles.Length}):\n\t{additionalFiles.Select(x => x.Name).DumpToTable()}");
                        foreach (var file in additionalFiles)
                        {
                            if (file is RefFileInfo)
                            {
                                continue;
                            }

                            WebView.FileProvider.FilesByName.AddOrUpdate(file);
                        }
                    }

                    var jsComponentsAccessor = new JSComponentConfigurationStoreAccessor(blazorContentRepository.JSComponents);
                    var webRootComponentsAccessor = new JSComponentConfigurationStoreAccessor(WebView.RootComponents.JSComponents);
                    foreach (var kvp in jsComponentsAccessor.JsComponentTypesByIdentifier)
                    {
                        if (webRootComponentsAccessor.JsComponentTypesByIdentifier.ContainsKey(kvp.Key))
                        {
                            continue;
                        }

                        webRootComponentsAccessor.RegisterForJavaScript(kvp.Value, kvp.Key);
                    }

                    var indexFileContent = PrepareIndexFileContext(indexFileContentTemplate, additionalFiles);
                    WebView.FileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo(generatedIndexFileName, Encoding.UTF8.GetBytes(indexFileContent), DateTimeOffset.Now));

                    if (WebView.HostPage == hostPage && WebView.WebView?.CoreWebView2 != null)
                    {
                        Log.Debug($"Reloading existing page, view type: {state}");
                        await Reload();
                    }
                    else
                    {
                        Log.Debug($"Navigating to index page, view type: {state}");
                        WebView.HostPage = hostPage;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to initialize view using {state}");
                    UnhandledException = e;
                }
                finally
                {
                    Disposable.Create(() => Log.Debug("Content has been disposed")).AddTo(contentAnchors);
                }
            })
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
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

    public Exception UnhandledException { get; private set; }

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

    private static string FormatExceptionMessage(Exception exception)
    {
        return $"{exception.GetType().Name}: {exception.Message} @ {exception.StackTrace}";
    }

    private void OnUnhandledException(object sender, WpfDispatcherUnhandlerExceptionEventArgs e)
    {
        switch (sender)
        {
            case BlazorWebView webView:
                webView.UnhandledException -= OnUnhandledException; //after webview crash it will be recreated anyways
                Log.Error(ReferenceEquals(sender, WebView.WebView) ? $"WebView has crashed: {sender}" : $"Obsolete(replaced) WebView has crashed: {sender}", e.Exception);
                break;
            case Dispatcher dispatcher:
                Log.Error($"WebView dispatcher has encountered an error", e.Exception);
                break;
            default:
                Log.Error($"WebView has encountered an error from {sender}", e.Exception);
                break;
        }
        
        UnhandledException = e.Exception;
        e.Handled = true; // JS context is already dead at this point
    }

    private static string PrepareIndexFileContext(string template, IReadOnlyList<IFileInfo> additionalFiles)
    {
        var cssLinksText = additionalFiles
            .Where(x => x.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.css", StringComparison.OrdinalIgnoreCase))
            .Select(file => $"""<link href="{file.Name}" rel="stylesheet"></link>""")
            .JoinStrings(Environment.NewLine);

        var scriptsText = additionalFiles
            .Where(x => x.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.js", StringComparison.OrdinalIgnoreCase))
            .Select(file => $"""<script src="{file.Name}"></script>""")
            .JoinStrings(Environment.NewLine);

        var indexFileContent = template
            .Replace("<!--% AdditionalStylesheetsBlock %-->", cssLinksText)
            .Replace("<!--% AdditionalScriptsBlock %-->", scriptsText);
        return indexFileContent;
    }
}