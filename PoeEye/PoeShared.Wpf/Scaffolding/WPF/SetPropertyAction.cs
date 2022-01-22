using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Interactivity;

namespace PoeShared.Scaffolding.WPF;

/// <summary>
///     Sets the designated property to the supplied value. TargetObject
///     optionally designates the object on which to set the property. If
///     TargetObject is not supplied then the property is set on the object
///     to which the trigger is attached.
/// </summary>
public sealed class SetPropertyAction : TriggerAction<FrameworkElement>
{
    public static readonly DependencyProperty PropertyNameProperty
        = DependencyProperty.Register(
            "PropertyName",
            typeof(string),
            typeof(SetPropertyAction));

    public static readonly DependencyProperty PropertyValueProperty
        = DependencyProperty.Register(
            "PropertyValue",
            typeof(object),
            typeof(SetPropertyAction));

    public static readonly DependencyProperty TargetObjectProperty
        = DependencyProperty.Register(
            "TargetObject",
            typeof(object),
            typeof(SetPropertyAction));

    /// <summary>
    ///     The property to be executed in response to the trigger.
    /// </summary>
    public string PropertyName
    {
        get => (string) GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    /// <summary>
    ///     The value to set the property to.
    /// </summary>
    public object PropertyValue
    {
        get => GetValue(PropertyValueProperty);
        set => SetValue(PropertyValueProperty, value);
    }


    /// <summary>
    ///     Specifies the object upon which to set the property.
    /// </summary>
    public object TargetObject
    {
        get => GetValue(TargetObjectProperty);
        set => SetValue(TargetObjectProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        var target = TargetObject ?? AssociatedObject;
        var propertyInfo = target.GetType()
            .GetProperty(
                PropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);

        if (propertyInfo == null)
        {
            throw new ApplicationException($"Could not find parameter {PropertyName} on object {target}");
        }

        var targetType = propertyInfo.PropertyType;
        var valueToSet = PropertyValue;
        if (PropertyValue is string && targetType != typeof(string))
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            valueToSet = converter?.ConvertFromInvariantString(PropertyValue as string);
        }

        propertyInfo.SetValue(target, valueToSet);
    }
}