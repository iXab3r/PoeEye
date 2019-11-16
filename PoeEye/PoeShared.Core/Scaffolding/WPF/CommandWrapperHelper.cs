using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Reactive.Linq;
using System.Windows.Controls.Primitives;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CommandWrapperHelper
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(object), typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon", typeof(object), typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
            "CommandParameter", typeof(object), typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button)
            {
                d.Observe<DependencyObject, CommandWrapper>(ButtonBase.CommandProperty)
                    .Take(1)
                    .Where(x => x != null)
                    .Subscribe(x => x.RaiseCanExecuteChanged());
            } else if (d is MenuItem)
            {
                d.SetCurrentValue(MenuItem.CommandParameterProperty, e.NewValue);
            }
        }

        public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.RegisterAttached(
            "IsDefault", typeof(bool), typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static void SetIcon(DependencyObject element, object value)
        {
            element.SetValue(IconProperty, value);
        }

        public static object GetIcon(DependencyObject element)
        {
            return element.GetValue(IconProperty);
        }

        public static void SetText(DependencyObject element, object value)
        {
            element.SetValue(TextProperty, value);
        }

        public static object GetText(DependencyObject element)
        {
            return (object) element.GetValue(TextProperty);
        }

        public static void SetCommandParameter(DependencyObject element, object value)
        {
            element.SetValue(CommandParameterProperty, value);
        }

        public static object GetCommandParameter(DependencyObject element)
        {
            return element.GetValue(CommandParameterProperty);
        }

        public static void SetIsDefault(DependencyObject element, bool value)
        {
            element.SetValue(IsDefaultProperty, value);
        }

        public static bool GetIsDefault(DependencyObject element)
        {
            return (bool) element.GetValue(IsDefaultProperty);
        }
    }
}