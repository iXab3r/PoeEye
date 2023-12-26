using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using DynamicData;
using PoeShared.Bindings;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Bindings;

internal sealed class BindingsEditorViewModel : DisposableReactiveObjectWithLogger, IBindingsEditorViewModel
{
    public BindingsEditorViewModel()
    {
        var bindablePropertiesSource = new SourceListEx<PropertyInfo>();
        bindablePropertiesSource
            .Connect()
            .Bind(out var bindableProperties)
            .SubscribeToErrors(Log.HandleUiException)
            .AddTo(Anchors);
        BindableProperties = bindableProperties;

        this.WhenAnyValue(x => x.Source)
            .SubscribeSafe(x =>
            {
                if (x == null)
                {
                    bindablePropertiesSource.Clear();
                    return;
                }

                var sourceProperties = x.GetType().GetProperties().Where(x => x.CanWrite).ToArray();

                var propertiesToAdd = sourceProperties.Except(bindablePropertiesSource.Items).ToArray();
                bindablePropertiesSource.AddRange(propertiesToAdd);
                    
                var propertiesToRemove = bindablePropertiesSource.Items.Except(sourceProperties).ToArray();
                bindablePropertiesSource.RemoveMany(propertiesToRemove);
            }, Log.HandleUiException)
            .AddTo(Anchors);
            
        AddBindingCommand = CommandWrapper.Create(AddBinding);
        RemoveBindingCommand = CommandWrapper.Create<object>(RemoveBinding);
    }

    public ReadOnlyObservableCollection<PropertyInfo> BindableProperties { get; }

    public DisposableReactiveObject ValueSource { get; set; }

    public string TargetProperty { get; set; }

    public string ValueSourceExpression { get; set; }

    public CommandWrapper AddBindingCommand { get; }

    public CommandWrapper RemoveBindingCommand { get; }

    public BindableReactiveObject Source { get; set; }

    private void AddBinding()
    {
        var sourcePath = string.IsNullOrEmpty(ValueSourceExpression) ? TargetProperty : ValueSourceExpression;
        var targetPath = TargetProperty;
        Log.Debug($"Adding new binding to {Source}: { new { targetPath, Source, sourcePath  } }");
        var binding = Source.AddOrUpdateBinding(targetPropertyPath: targetPath, source: ValueSource, sourcePropertyPath: sourcePath);
        Log.Debug($"Added new binding: {binding}");
    }
    
    private void RemoveBinding(object arg)
    {
        if (arg is string targetPropertyPath)
        {
            Log.Debug($"Removing binding of {Source}, path: {targetPropertyPath}");
            Source.RemoveBinding(targetPropertyPath);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(arg), $"Unknown argument: {arg}");
        }
    }
}