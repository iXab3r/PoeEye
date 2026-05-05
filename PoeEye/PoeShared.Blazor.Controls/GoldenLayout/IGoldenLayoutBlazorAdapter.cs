using System;
using System.Threading.Tasks;
using DynamicData;

namespace PoeShared.Blazor.Controls.GoldenLayout;

public interface IGoldenLayoutBlazorAdapter : IDisposable
{
    ValueTask Initialize(IGoldenLayoutFacade glFacade);
    
    IObservableCache<GLBlazorComponent, string> ComponentsById { get; }
    
    ValueTask<GLBlazorComponent> Add(
        GLBlazorComponentState state,
        object? dataContext = null,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null);

    ValueTask<GLBlazorComponent> AddAtLocation(
        GoldenLayoutLocationSelector locationSelector,
        GLBlazorComponentState state,
        object? dataContext = null,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null);
    
    ValueTask<GLBlazorComponent> AddChild(
        string id,
        GLBlazorComponentState state,
        object? dataContext = null,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null);

    ValueTask Remove(string id);
}
