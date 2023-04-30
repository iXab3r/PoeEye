using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Web.WebView2.Wpf;
using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.Services;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

public class BlazorContentControl : ReactiveControl
{
    private static readonly IFluentLog Log = typeof(BlazorContentControl).PrepareLogger();

    private static readonly Binder<BlazorContentControl> Binder = new();

    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

    public static readonly DependencyProperty AdditionalFilesProperty = DependencyProperty.Register(
        nameof(AdditionalFiles), typeof(IEnumerable<IFileInfo>), typeof(BlazorContentControl), new PropertyMetadata(default(IEnumerable<IFileInfo>)));

    private readonly ISharedResourceLatch isBusyLatch;

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
        Binder.Bind(x => x.isBusyLatch.IsBusy).To(x => x.IsBusy);
    }

    public BlazorContentControl()
    {
        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        OpenDevTools = CommandWrapper.Create(() => WebView?.WebView.CoreWebView2.OpenDevToolsWindow());

        ReloadCommand = CommandWrapper.Create(() =>
        {
            if (UnhandledException != null)
            {
                Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                UnhandledException = null;
            }

            WebView?.WebView.Reload();
        });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddWpfBlazorWebView();
        //FIXME Singleton seems to be the simplest way to link WPF world to ASPNETCORE
        foreach (var serviceDescriptor in BlazorServiceCollection.Instance)
        {
            serviceCollection.Add(serviceDescriptor);
        }

        serviceCollection.AddSingleton<IComponentActivator, BlazorComponentActivator>();

        var indexFileContentTemplate = ResourceReader.ReadResourceAsString(Assembly.GetExecutingAssembly(), @"wwwroot.index.html");

        this.WhenAnyValue(x => x.ViewType)
            .ObserveOnDispatcher()
            .Subscribe(async viewType =>
            {
                using var rent = isBusyLatch.Rent();

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (WebView != null)
                {
                    Log.Debug("Disposing previous instance of WebView");
                    await WebView.DisposeAsync();
                    WebView = null;

                    if (View is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    View = null;
                }

                if (viewType == null)
                {
                    return;
                }

                // views have to be transient to allow to re-create them if needed (e.g. on error)
                serviceCollection.AddTransient(typeof(BlazorContent), x =>
                {
                    var wrapper = new BlazorContent
                    {
                        Type = viewType
                    };
                    return wrapper;
                });
                serviceCollection.AddTransient(viewType, x =>
                {
                    var view = Activator.CreateInstance(viewType);
                    if (view is BlazorReactiveComponent reactiveComponent)
                    {
                        this.WhenAnyValue(x => x.Content)
#pragma warning disable BL0005 // this is a special case
                            .Subscribe(x => reactiveComponent.DataContext = x)
#pragma warning restore BL0005
                            .AddTo(reactiveComponent.Anchors);
                    }

                    return view;
                });

                var webView = new BlazorWebViewEx();
                webView.UnhandledException += OnUnhandledException;
                webView.Services = serviceCollection.BuildServiceProvider();

                new RootComponent
                {
                    Selector = "headOutlet",
                    ComponentType = typeof(HeadOutlet)
                }.AddTo(webView.RootComponents);

                new RootComponent
                {
                    Selector = "#app",
                    ComponentType = typeof(BlazorContent)
                }.AddTo(webView.RootComponents);

                var additionalFiles = AdditionalFiles?.ToArray() ?? Array.Empty<IFileInfo>();
                webView.FileProvider.FilesByName.AddOrUpdate(additionalFiles);

                var cssLinksText = additionalFiles
                    .Where(x => x.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.css", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"""<link href="{x.Name}" rel="stylesheet"></link>""")
                    .JoinStrings(Environment.NewLine);
                
                var scriptsText = additionalFiles
                    .Where(x => x.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase) && !x.Name.EndsWith(".usr.js", StringComparison.OrdinalIgnoreCase))
                    .Select(x => $"""<script src="{x.Name}"></script>""")
                    .JoinStrings(Environment.NewLine);

                var indexFileContent = indexFileContentTemplate
                    .Replace("<!--% AdditionalStylesheetsBlock %-->", cssLinksText)
                    .Replace("<!--% AdditionalScriptsBlock %-->", scriptsText);
                
                webView.FileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo("index.g.html", Encoding.UTF8.GetBytes(indexFileContent), DateTimeOffset.Now));
                webView.HostPage = "wwwroot/index.g.html"; // wwwroot must be included as a part of path to become ContentRoot
                WebView = webView;
            })
            .AddTo(Anchors);

        Binder.Attach(this).AddTo(Anchors);
    }

    public ICommand OpenDevTools { get; }

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

    public IEnumerable<IFileInfo> AdditionalFiles
    {
        get => (IEnumerable<IFileInfo>) GetValue(AdditionalFilesProperty);
        set => SetValue(AdditionalFilesProperty, value);
    }

    public bool IsBusy { get; [UsedImplicitly] private set; }

    /// <summary>
    ///     We have to dynamically recreate WebView when needed as it is EXTREMELY unfriendly for any changes of associated
    ///     properties
    /// </summary>
    public BlazorWebViewEx WebView { get; private set; }

    public Exception UnhandledException { get; private set; }

    public object View { get; private set; }

    public ICommand ReloadCommand { get; }

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
}