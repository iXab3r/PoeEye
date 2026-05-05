using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Kernel;
using PoeShared.Blazor;
using PoeShared.Blazor.Controls.Services;
using PoeShared.Scaffolding;
using PoeShared.Services;

namespace PoeShared.Blazor.Controls.GoldenLayout;

internal sealed class GLBlazorBlazorAdapter : DisposableReactiveObject, IGoldenLayoutBlazorAdapter
{
    private IGoldenLayoutFacade? glFacade;
    private readonly IUniqueIdGenerator idGenerator;
    private readonly IDynamicComponentParameterStorage dynamicComponentParameterStorage;
    private readonly SourceCache<GLBlazorComponent, string> componentsById = new(x => x.ComponentState.Id!);

    public GLBlazorBlazorAdapter(
        IUniqueIdGenerator idGenerator,
        IDynamicComponentParameterStorage dynamicComponentParameterStorage)
    {
        this.idGenerator = idGenerator;
        this.dynamicComponentParameterStorage = dynamicComponentParameterStorage;
    }

    public async ValueTask Initialize(IGoldenLayoutFacade newGLFacade)
    {
        if (glFacade != null)
        {
            throw new InvalidOperationException($"GoldenLayoutBlazorAdapter can only be initialized once and it is already bound to {this.glFacade}");
        }
        glFacade = newGLFacade ?? throw new ArgumentNullException(nameof(newGLFacade));
    }

    public IObservableCache<GLBlazorComponent, string> ComponentsById => componentsById;

    public async ValueTask<GLBlazorComponent> Add(
        GLBlazorComponentState state,
        object? dataContext,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null)
    {
        var facade = EnsureReady();
        var componentInfo = PrepareComponent(state, dataContext, bodyViewType: bodyViewType, headerViewType: headerViewType, bodyViewTemplateKey, headerViewTemplateKey);
        var location = await facade.AddBlazorItem(componentInfo.ComponentState);
        componentsById.AddOrUpdate(componentInfo);
        return componentInfo;
    }
    
    public async ValueTask<GLBlazorComponent> AddAtLocation(
        GoldenLayoutLocationSelector locationSelector,
        GLBlazorComponentState state,
        object? dataContext = null,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null)
    {
        var facade = EnsureReady();
        var componentInfo = PrepareComponent(state, dataContext, bodyViewType: bodyViewType, headerViewType: headerViewType, bodyViewTemplateKey, headerViewTemplateKey);
        var location = await facade.AddBlazorItemAtLocation(componentInfo.ComponentState, locationSelector);
        componentsById.AddOrUpdate(componentInfo);
        return componentInfo;
    }

    public async ValueTask<GLBlazorComponent> AddChild(
        string id, 
        GLBlazorComponentState state, 
        object? dataContext = null, 
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null, 
        object? headerViewTemplateKey = null)
    {
        var facade = EnsureReady();
        var componentInfo = PrepareComponent(state, dataContext, bodyViewType: bodyViewType, headerViewType: headerViewType, bodyViewTemplateKey, headerViewTemplateKey);
        var location = await facade.AddBlazorChildItem(id, componentInfo.ComponentState);
        componentsById.AddOrUpdate(componentInfo);
        return componentInfo;
    }

    public async ValueTask Remove(string id)
    {
        var facade = EnsureReady();
        var componentInfo = componentsById.Lookup(id).ValueOrThrow(() => new ArgumentException($"Could not find component with Id {id} (total: {componentsById.Count})"));
        await facade.RemoveItemById(componentInfo.ComponentState.Id!);
        componentsById.Remove(componentInfo);

        var bodyId = componentInfo.ComponentState.DynamicComponentId;
        if (bodyId != default)
        {
            dynamicComponentParameterStorage.Unregister(bodyId);
        }

        var tabId = componentInfo.ComponentState.TabDynamicComponentId;
        if (tabId.HasValue && tabId.Value != default)
        {
            dynamicComponentParameterStorage.Unregister(tabId.Value);
        }
    }

    private IGoldenLayoutFacade EnsureReady()
    {
        if (glFacade == null)
        {
            throw new InvalidOperationException("GoldenLayoutBlazorAdapter has not been initialized yet");
        }

        return glFacade;
    }

    private GLBlazorComponent PrepareComponent(
        GLBlazorComponentState state,
        object? dataContext,
        Type? bodyViewType = null,
        Type? headerViewType = null,
        object? bodyViewTemplateKey = null,
        object? headerViewTemplateKey = null)
    {
        var componentId = string.IsNullOrEmpty(state.Id) ? idGenerator.Next("BC") : state.Id;
        var modifiedState = state with
        {
            Id = componentId,
            DynamicComponentId = dynamicComponentParameterStorage.Register<BlazorContentPresenter>(new Dictionary<string, object?>()
            {
                {nameof(BlazorContentPresenter.Content), dataContext},
                {nameof(BlazorContentPresenter.ViewType), bodyViewType},
                {nameof(BlazorContentPresenter.ViewTypeKey), bodyViewTemplateKey}
            }),
            TabDynamicComponentId = headerViewTemplateKey == null ? default : dynamicComponentParameterStorage.Register<BlazorContentPresenter>(new Dictionary<string, object?>()
            {
                {nameof(BlazorContentPresenter.Content), dataContext},
                {nameof(BlazorContentPresenter.ViewType), headerViewType},
                {nameof(BlazorContentPresenter.ViewTypeKey), headerViewTemplateKey}
            }),
        };

        var componentInfo = new GLBlazorComponent(modifiedState, dataContext, bodyViewType, headerViewType, bodyViewTemplateKey, headerViewTemplateKey);
        return componentInfo;
    }
}
