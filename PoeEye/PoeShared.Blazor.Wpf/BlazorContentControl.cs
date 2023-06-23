using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DynamicData;
using GongSolutions.Wpf.DragDrop.Utilities;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;

namespace PoeShared.Blazor.Wpf;

public class BlazorContentControl : ReactiveControl, IBlazorContentControl
{
    private static readonly IFluentLog Log = typeof(BlazorContentControl).PrepareLogger();

    private static readonly Binder<BlazorContentControl> Binder = new();

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
    private readonly SerialDisposable activeViewAnchors;
    private readonly ProxyServiceProvider proxyServiceProvider;

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
        activeViewAnchors = new SerialDisposable().AddTo(Anchors);
        proxyServiceProvider = new ProxyServiceProvider().AddTo(Anchors);

        WebView = new BlazorWebViewEx();
        WebView.UnhandledException += OnUnhandledException;

        var canExecuteHotkeys = this.Observe(EnableHotkeysProperty, x => x.EnableHotkeys)
            .Select(x => x);

        ReloadCommand = CommandWrapper.Create(() =>
        {
            if (UnhandledException != null)
            {
                Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                UnhandledException = null;
            }

            WebView?.WebView.Reload();
        }, canExecuteHotkeys);
        OpenDevTools = CommandWrapper.Create(() => WebView?.WebView.CoreWebView2.OpenDevToolsWindow(), canExecuteHotkeys);

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddWpfBlazorWebView();
        //FIXME Singleton seems to be the simplest way to link WPF world to ASPNETCORE
        foreach (var serviceDescriptor in BlazorServiceCollection.Instance)
        {
            serviceCollection.Add(serviceDescriptor);
        }

        serviceCollection.AddTransient<IComponentActivator>(_ => new BlazorComponentActivator(proxyServiceProvider));
        //Root component is NEVER instantiated again

        proxyServiceProvider.ServiceProvider = serviceCollection.BuildServiceProvider();

        WebView.Services = proxyServiceProvider;

        new RootComponent
        {
            Selector = "headOutlet",
            ComponentType = typeof(HeadOutlet)
        }.AddTo(WebView.RootComponents);

        new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(BlazorContent)
        }.AddTo(WebView.RootComponents);

        var indexFileContentTemplate = ResourceReader.ReadResourceAsString(Assembly.GetExecutingAssembly(), @"wwwroot.index.html");
        var generatedIndexFileName = "index.g.html";
        var contentRoot = "wwwroot";
        var hostPage = Path.Combine(contentRoot, generatedIndexFileName); // wwwroot must be included as a part of path to become ContentRoot;

        this.WhenAnyValue(x => x.ViewType)
            .CombineLatest(this.WhenAnyValue(x => x.Container), (viewType, container) => new {viewType, container})
            .ObserveOnDispatcher()
            .SubscribeAsync(async state =>
            {
                using var rent = isBusyLatch.Rent();

                Log.Debug(() => $"Reloading control, new content type: {state.viewType}");

                var viewAnchors = new CompositeDisposable().AssignTo(activeViewAnchors);
                WebView.FileProvider.FilesByName.Clear();

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (state?.viewType == null)
                {
                    Log.Debug(() => $"Content type is not specified, nothing to load");
                    return;
                }

                try
                {
                    var childServiceCollection = new ServiceCollection
                    {
                        serviceCollection
                    };

                    // views have to be transient to allow to re-create them if needed (e.g. on error)
                    childServiceCollection.AddTransient(typeof(BlazorContent), _ =>
                    {
                        var viewWrapper = new BlazorContent(state.viewType).AddTo(viewAnchors);
                        return viewWrapper;
                    });

                    childServiceCollection.AddTransient(state.viewType, _ =>
                    {
                        var view = state.container == null ? Activator.CreateInstance(state.viewType) : state.container.Resolve(state.viewType);
                        if (view is BlazorReactiveComponent reactiveComponent)
                        {
                            this.WhenAnyValue(content => content.Content)
#pragma warning disable BL0005 // this is a special case
                                .Subscribe(content => reactiveComponent.DataContext = content)
#pragma warning restore BL0005
                                .AddTo(viewAnchors);
                        } 
                        
                        if (view is IDisposable disposable)
                        {
                            Disposable.Create(() =>
                            {
                                Log.Debug($"Disposing {view}");
                                disposable.Dispose();
                            }).AddTo(viewAnchors);
                        }

                        return view;
                    });
                    proxyServiceProvider.ServiceProvider = childServiceCollection.BuildServiceProvider();

                    var additionalFiles = AdditionalFiles?.ToArray() ?? Array.Empty<IFileInfo>();
                    if (additionalFiles.Any())
                    {
                        Log.Debug(() => $"Loading additional files: {additionalFiles.Select(x => x.Name).DumpToString()}");
                        WebView.FileProvider.FilesByName.AddOrUpdate(additionalFiles);
                    }

                    var indexFileContent = PrepareIndexFileContext(indexFileContentTemplate, additionalFiles);
                    WebView.FileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo(generatedIndexFileName, Encoding.UTF8.GetBytes(indexFileContent), DateTimeOffset.Now));


                    if (WebView.HostPage == hostPage && WebView.WebView.CoreWebView2 != null)
                    {
                        Log.Debug($"Reloading existing page, view type: {state}");
                        WebView.WebView.Reload();
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

    public ICommand ReloadCommand { get; }

    public ICommand OpenDevTools { get; }

    private static string FormatExceptionMessage(Exception exception)
    {
        return exception.ToString();
    }

    private void OnUnhandledException(object sender, WpfDispatcherUnhandlerExceptionEventArgs e)
    {
        if (sender is BlazorWebView webView)
        {
            webView.UnhandledException -= OnUnhandledException;
        }

        Log.Error($"WebView has crashed: {sender}", e.Exception);
        e.Handled = true; // JS context is already dead at this point
        UnhandledException = e.Exception;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.OriginalSource is not WebView2)
        {
            base.OnKeyDown(e);
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.OriginalSource is not WebView2)
        {
            base.OnKeyUp(e);
        }
    }

    private static string PrepareIndexFileContext(string template, IReadOnlyList<IFileInfo> additionalFiles)
    {
        var cssLinksText = additionalFiles
            .Where(x => x.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.css", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"""<link href="{x.Name}" rel="stylesheet"></link>""")
            .JoinStrings(Environment.NewLine);

        var scriptsText = additionalFiles
            .Where(x => x.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.js", StringComparison.OrdinalIgnoreCase))
            .Select(x => $"""<script src="{x.Name}"></script>""")
            .JoinStrings(Environment.NewLine);

        var indexFileContent = template
            .Replace("<!--% AdditionalStylesheetsBlock %-->", cssLinksText)
            .Replace("<!--% AdditionalScriptsBlock %-->", scriptsText);
        return indexFileContent;
    }
}