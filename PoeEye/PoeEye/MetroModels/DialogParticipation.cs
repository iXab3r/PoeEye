namespace PoeEye.MetroModels
{
    using System.Collections.Generic;
    using System.Windows;

    using Guards;

    internal static class DialogParticipation
    {
        private static readonly IDictionary<object, DependencyObject> ContextRegistrationIndex = new Dictionary<object, DependencyObject>();

        public static readonly DependencyProperty RegisterProperty = DependencyProperty.RegisterAttached(
            "Register",
            typeof (object),
            typeof (DialogParticipation),
            new PropertyMetadata(default(object), RegisterPropertyChangedCallback));

        private static void RegisterPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            if (dependencyPropertyChangedEventArgs.OldValue != null)
            {
                ContextRegistrationIndex.Remove(dependencyPropertyChangedEventArgs.OldValue);
            }

            if (dependencyPropertyChangedEventArgs.NewValue != null)
            {
                ContextRegistrationIndex[dependencyPropertyChangedEventArgs.NewValue] = dependencyObject;
            }
        }

        public static void SetRegister(DependencyObject element, object context)
        {
            element.SetValue(RegisterProperty, context);
        }

        public static object GetRegister(DependencyObject element)
        {
            return element.GetValue(RegisterProperty);
        }

        internal static bool IsRegistered(object context)
        {
            Guard.ArgumentNotNull(() => context);
            
            return ContextRegistrationIndex.ContainsKey(context);
        }

        internal static DependencyObject GetAssociation(object context)
        {
            Guard.ArgumentNotNull(() => context);

            return ContextRegistrationIndex[context];
        }
    }
}