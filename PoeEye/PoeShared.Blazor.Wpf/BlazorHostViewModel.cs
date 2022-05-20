using System;
using System.Reactive.Linq;
using DynamicData;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.DependencyInjection;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Blazor.Wpf;

public class BlazorHostViewModel : DisposableReactiveObject
{
    public BlazorHostViewModel()
    {
        Components = new SourceListEx<Type>();
        this.WhenAnyValue(x => x.View)
            .Where(x => x != null)
            .Take(1)
            .Subscribe(HandleViewInitialization)
            .AddTo(Anchors);
    }

    public ISourceList<Type> Components { get; }

    public BlazorWebView View { get; set; }

    private void HandleViewInitialization(BlazorWebView view)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddBlazorWebView();
        view.Services = serviceCollection.BuildServiceProvider();

        Components
            .Connect()
            .OnItemAdded(x =>
            {
                var rootComponent = new RootComponent()
                {
                    Selector = "#app",
                    ComponentType = x
                };
                view.RootComponents.Add(rootComponent);
            })
            .Subscribe()
            .AddTo(Anchors);


        view.HostPage = "wwwroot4wpf/index.html";
    }
}