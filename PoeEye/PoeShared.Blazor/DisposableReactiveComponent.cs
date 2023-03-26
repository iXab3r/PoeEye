using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynamicData.Binding;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor;

public abstract class DisposableReactiveComponent : DisposableReactiveObjectWithLogger, IHasError
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CollectionProperties = new();

    public IStringLocalizer Localizer { get; set; }

    public IJSRuntime JsRuntime { get; set; }

    public IHttpContextAccessor HttpContextAccessor { get; set; }
    
    public NavigationManager NavigationManager { get; set; }
    
    public IDictionary<string, StringValues> QueryParameters { get; set; }

    public string Error { get; protected set; }

    public async Task OnParametersSetAsync()
    {
        await HandleParametersSetAsync();
    }
    
    public async Task OnInitializedAsync()
    {
        await HandleInitializedAsync();

        var properties = CollectionProperties.GetOrAdd(this.GetType(), type => type
            .GetAllProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => typeof(INotifyCollectionChanged).IsAssignableFrom(x.PropertyType))
            .Where(x => x.CanRead)
            .ToArray());
        if (properties.Any())
        {
            properties.Select(property =>
                {
                    return this.WhenAnyProperty(property.Name)
                        .StartWithDefault()
                        .Select(_ => property.GetValue(this) as INotifyCollectionChanged)
                        .Select(x => x.ObserveCollectionChanges())
                        .Switch()
                        .Select(x => new {property.Name, x.EventArgs.Action, x.EventArgs});
                }).Merge()
                .SubscribeSafe(x => RaisePropertyChanged(x.Name), Log.HandleException)
                .AddTo(Anchors);
        }
    }

    public async Task OnAfterRenderAsync(bool firstRender)
    {
        await HandleAfterRenderAsync(firstRender);
    }

    protected virtual async Task HandleAfterRenderAsync(bool firstRender)
    {
    }

    protected virtual async Task HandleInitializedAsync()
    {
    }
        
    protected virtual async Task HandleParametersSetAsync()
    {
    }
    

    protected virtual async Task<FluentValidation.Results.ValidationResult> Validate()
    {
        return new();
    }
        
    public async Task HandleSubmit(EditContext context)
    {
        try
        {
            var validationResult = await Validate();
            var contextResult = new { IsValid = context.Validate(), Error= context.GetValidationMessages().ToArray() };
            if (!validationResult.IsValid)
            {
                context.NotifyValidationStateChanged();
                return;
            }
            await HandleSubmitInternal(context);
            await HandleValidSubmit();
        }
        catch (Exception e)
        {
            //FIXME Due to problems with async validation it's safer to handle possible exceptions here
            Log.Error("Something went wrong during Submit operation", e);
            Error = e.Message;
        }
    }

    protected virtual async Task HandleValidSubmit()
    {
    }

    protected virtual async Task HandleSubmitInternal(EditContext context)
    {
    } 
}