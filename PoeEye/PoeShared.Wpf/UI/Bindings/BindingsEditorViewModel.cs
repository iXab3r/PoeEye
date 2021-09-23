using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using DynamicData;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace PoeShared.UI.Bindings
{
    internal sealed class BindingsEditorViewModel : DisposableReactiveObject, IBindingsEditorViewModel
    {
        private static readonly IFluentLog Log = typeof(BindingsEditorViewModel).PrepareLogger();

        private string valueSourceExpression;

        private DisposableReactiveObject valueSource;

        private BindableReactiveObject source;

        private string targetProperty;

        public BindingsEditorViewModel()
        {
            var bindablePropertiesSource = new SourceList<PropertyInfo>();
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

        private void RemoveBinding(object arg)
        {
            if (arg is string propertyName)
            {
                Source.RemoveBinding(propertyName);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(arg), $"Unknown argument: {arg}");
            }
        }

        public ReadOnlyObservableCollection<PropertyInfo> BindableProperties { get; }

        public DisposableReactiveObject ValueSource
        {
            get => valueSource;
            set => RaiseAndSetIfChanged(ref valueSource, value);
        }

        public string TargetProperty
        {
            get => targetProperty;
            set => RaiseAndSetIfChanged(ref targetProperty, value);
        }

        public string ValueSourceExpression
        {
            get => valueSourceExpression;
            set => RaiseAndSetIfChanged(ref valueSourceExpression, value);
        }

        public CommandWrapper AddBindingCommand { get; }

        public CommandWrapper RemoveBindingCommand { get; }

        public BindableReactiveObject Source
        {
            get => source;
            set => RaiseAndSetIfChanged(ref source, value);
        }

        private void AddBinding()
        {
            var binding = Source.AddOrUpdateBinding(targetProperty, ValueSource, string.IsNullOrEmpty(valueSourceExpression) ? targetProperty : valueSourceExpression);
        }
    }
}