﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
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
    private readonly SerialDisposable activeViewAnchors;
    private readonly ProxyServiceProvider proxyServiceProvider;

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
        Binder.Bind(x => x.isBusyLatch.IsBusy).To(x => x.IsBusy);
    }

    public BlazorContentControl()
    {
        isBusyLatch = new SharedResourceLatch().AddTo(Anchors);
        activeViewAnchors = new SerialDisposable().AddTo(Anchors);
        proxyServiceProvider = new ProxyServiceProvider().AddTo(Anchors);

        WebView = new BlazorWebViewEx();
        WebView.UnhandledException += OnUnhandledException;

        ReloadCommand = CommandWrapper.Create(() =>
        {
            if (UnhandledException != null)
            {
                Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                UnhandledException = null;
            }

            WebView?.WebView.Reload();
        });
        OpenDevTools = CommandWrapper.Create(() => WebView?.WebView.CoreWebView2.OpenDevToolsWindow());

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
            .ObserveOnDispatcher()
            .SubscribeAsync(async viewType =>
            {
                if (WebView.WebView is {CoreWebView2: not null})
                {
                    ViewScreenshot = await WebView.TakeScreenshotAsBitmapSource();
                }

                using var rent = isBusyLatch.Rent();

                var viewAnchors = new CompositeDisposable().AssignTo(activeViewAnchors);
                WebView.FileProvider.FilesByName.Clear();

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (viewType == null)
                {
                    return;
                }

                var childContainer = new ServiceCollection
                {
                    serviceCollection
                };
                
                // views have to be transient to allow to re-create them if needed (e.g. on error)
                childContainer.AddTransient(typeof(BlazorContent), _ =>
                {
                    var viewWrapper = new BlazorContent(viewType).AddTo(viewAnchors);
                    return viewWrapper;
                });
                
                childContainer.AddTransient(viewType, _ =>
                {
                    var view = Activator.CreateInstance(viewType);
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
                        viewAnchors.Add(disposable);
                    }

                    return view;
                });
                proxyServiceProvider.ServiceProvider = childContainer.BuildServiceProvider();

                var additionalFiles = AdditionalFiles?.ToArray() ?? Array.Empty<IFileInfo>();
                WebView.FileProvider.FilesByName.AddOrUpdate(additionalFiles);

                var indexFileContent = PrepareIndexFileContext(indexFileContentTemplate, additionalFiles);
                WebView.FileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo(generatedIndexFileName, Encoding.UTF8.GetBytes(indexFileContent), DateTimeOffset.Now));
                if (WebView.HostPage == hostPage)
                {
                    Log.Debug($"Reloading existing page, view type: {viewType}");
                    WebView.WebView.Reload();
                }
                else
                {
                    Log.Debug($"Navigating to index page, view type: {viewType}");
                    WebView.HostPage = hostPage;
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
    public BlazorWebViewEx WebView { get; }

    public Exception UnhandledException { get; private set; }
    
    public ICommand ReloadCommand { get; }

    public ICommand OpenDevTools { get; }
    
    public BitmapSource ViewScreenshot { get; private set; }

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