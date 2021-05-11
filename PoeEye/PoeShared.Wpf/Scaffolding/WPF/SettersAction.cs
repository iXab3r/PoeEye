using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Markup;

namespace PoeShared.Scaffolding.WPF
{
    [ContentProperty("Setters")]
    internal sealed class SettersAction : TriggerAction<FrameworkElement>
    {
        private readonly SetterBaseCollection setters = new SetterBaseCollection();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public SetterBaseCollection Setters
        {
            get
            {
                VerifyAccess();
                return setters;
            }
        }

        protected override void Invoke(object parameter)
        {
            foreach (var untypedSetter in Setters)
            {
                var setter = untypedSetter as Setter;
                if (setter == null)
                {
                    throw new NotSupportedException($"Only Setter type is supported, got {untypedSetter}");
                }

                var element = string.IsNullOrEmpty(setter.TargetName)
                    ? AssociatedObject
                    : AssociatedObject.FindName(setter.TargetName) as DependencyObject;
                if (element == null)
                {
                    continue;
                }

                element.SetValue(setter.Property, setter.Value);
            }
        }
    }
}