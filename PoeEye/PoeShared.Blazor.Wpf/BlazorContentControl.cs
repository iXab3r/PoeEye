using System;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

public class BlazorContentControl : ReactiveControl
{
    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
    }

    public BlazorContentControl()
    {
        OpenDevTools = CommandWrapper.Create(() => WebView?.WebView.CoreWebView2.OpenDevToolsWindow());   
        
        this.WhenAnyValue(x => x.ViewType)
            .ObserveOnDispatcher()
            .Subscribe(async viewType =>
            {
                if (WebView != null)
                {
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
                
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddBlazorWebView();
                serviceCollection.AddWpfBlazorWebView();
                foreach (var serviceDescriptor in BlazorServiceCollection.Instance)
                {
                    serviceCollection.Add(serviceDescriptor);
                }

                serviceCollection.AddSingleton<IComponentActivator, BlazorComponentActivator>();
                var view = Activator.CreateInstance(viewType) ?? throw new ArgumentNullException();
                serviceCollection.TryAdd(new ServiceDescriptor(viewType, view));
                
                var webView = new BlazorWebViewEx();
                webView.Services = serviceCollection.BuildServiceProvider();
                new RootComponent
                {
                    Selector = $"#app",
                    ComponentType = viewType
                }.AddTo(webView.RootComponents);
                webView.Initialized += WebViewOnInitialized;
                webView.HostPage = "index.html";
                
                View = view;
                WebView = webView;
            })
            .AddTo(Anchors);
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
    
    /// <summary>
    /// We have to dynamically recreate WebView when needed as it is EXTREMELY unfriendly for any changes of associated properties
    /// </summary>
    public BlazorWebViewEx WebView { get; private set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        this.WhenAnyValue(x => x.Content, x => x.View)
            .Subscribe(x =>
            {
                if (x.Item2 is BlazorReactiveComponent blazorReactiveComponent)
                {
                    blazorReactiveComponent.DataContext = x.Item1;
                }
            })
            .AddTo(Anchors);
    }

    private void WebViewOnInitialized(object sender, EventArgs e)
    {
        var webView = (BlazorWebViewEx)sender;
        webView.Initialized -= WebViewOnInitialized;
    }

    public object View { get; private set; }
}