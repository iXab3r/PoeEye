using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Web.WebView2.Wpf;
using PoeShared.Logging;
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

    private readonly ISharedResourceLatch isBusyLatch;

    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

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

        this.WhenAnyValue(x => x.ViewType)
            .ObserveOnDispatcher()
            .Subscribe(async viewType =>
            {
                using var @rent = isBusyLatch.Rent();

                if (UnhandledException != null)
                {
                    Log.Debug($"Erasing previous unhandled exception: {UnhandledException.Message}");
                    UnhandledException = null;
                }

                if (WebView != null)
                {
                    Log.Debug($"Disposing previous instance of WebView");
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
                    var wrapper = new BlazorContent()
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
                    Selector = $"headOutlet",
                    ComponentType = typeof(HeadOutlet)
                }.AddTo(webView.RootComponents);

                new RootComponent
                {
                    Selector = $"#app",
                    ComponentType = typeof(BlazorContent)
                }.AddTo(webView.RootComponents);

                webView.HostPage = "wwwroot/index.html";
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

    public bool IsBusy { get; private set; }

    /// <summary>
    /// We have to dynamically recreate WebView when needed as it is EXTREMELY unfriendly for any changes of associated properties
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