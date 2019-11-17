using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class CommandWrapperHelper
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text",
            typeof(object),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
            "Icon",
            typeof(object),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits,
                PropertyChangedCallback));

        public static readonly DependencyProperty DataContextProperty = DependencyProperty.RegisterAttached(
            "DataContext",
            typeof(object),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.RegisterAttached(
            "IsDefault",
            typeof(bool),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static readonly DependencyProperty ProgressProperty = DependencyProperty.RegisterAttached(
            "Progress",
            typeof(int),
            typeof(CommandWrapperHelper),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

        public static void SetProgress(DependencyObject element, int value)
        {
            element.SetValue(ProgressProperty, value);
        }

        public static int GetProgress(DependencyObject element)
        {
            return (int) element.GetValue(ProgressProperty);
        }

        public static void SetDataContext(DependencyObject element, object value)
        {
            element.SetValue(DataContextProperty, value);
        }

        public static object GetDataContext(DependencyObject element)
        {
            return element.GetValue(DataContextProperty);
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button)
            {
                d.Observe<DependencyObject, CommandWrapper>(ButtonBase.CommandProperty)
                    .Take(1)
                    .Where(x => x != null)
                    .Subscribe(x => x.RaiseCanExecuteChanged());
            }
            else if (d is MenuItem)
            {
                d.SetCurrentValue(MenuItem.CommandParameterProperty, e.NewValue);
            }
        }

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
            return element.GetValue(TextProperty);
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