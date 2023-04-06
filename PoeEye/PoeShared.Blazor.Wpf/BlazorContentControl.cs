using System;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PoeShared.Scaffolding;
using PoeShared.UI;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

[TemplatePart(Name = PART_WebView, Type = typeof(CheckBox))]
public class BlazorContentControl : ReactiveControl
{
    public const string PART_WebView = "PART_WebView";

    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(
        nameof(ViewType), typeof(Type), typeof(BlazorContentControl), new PropertyMetadata(default(Type)));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(BlazorContentControl), new PropertyMetadata(default(object)));

    private BlazorWebView webView;

    static BlazorContentControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BlazorContentControl), new FrameworkPropertyMetadata(typeof(BlazorContentControl)));
    }

    public BlazorContentControl()
    {
        
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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        webView = Template.FindName(PART_WebView, this) as BlazorWebView;
        if (webView == null)
        {
            return;
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        serviceCollection.AddWpfBlazorWebView();
        serviceCollection.TryAdd<IComponentActivator, BlazorComponentActivator>(ServiceLifetime.Singleton);
        webView.Services = serviceCollection.BuildServiceProvider();

        this.WhenAnyValue(x => x.ViewType)
            .Subscribe(viewType =>
            {
                webView.RootComponents.Clear();
                if (viewType == null)
                {
                    return;
                }
                
                View = (BlazorReactiveComponent)Activator.CreateInstance(viewType) ?? throw new ArgumentNullException();
                serviceCollection.RemoveAll(viewType);
                serviceCollection.TryAdd(new ServiceDescriptor(viewType, View));
                webView.Services = serviceCollection.BuildServiceProvider();
                var rootComponent =  new RootComponent
                {
                    Selector = "#app",
                    ComponentType = viewType
                };
                webView.RootComponents.Add(rootComponent);
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.Content, x => x.View)
            .Subscribe(x =>
            {
                if (x.Item2 != null)
                {
                    x.Item2.DataContext = x.Item1;
                }
            })
            .AddTo(Anchors);
        

        webView.HostPage = "wwwroot/_Host.html";
    }
    
    public BlazorReactiveComponent View { get; private set; }
}