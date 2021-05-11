using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace PoeShared.Scaffolding.WPF
{
    /// <summary>
    /// Creates a bindable attached property for the <see cref="PasswordBox.SecurePassword"/> property.
    /// </summary>
    public static class PasswordBoxHelper
    {
        // an attached behavior won't work due to view model validation not picking up the right control to adorn
        public static readonly DependencyProperty SecurePasswordBindingProperty = DependencyProperty.RegisterAttached(
            "SecurePassword",
            typeof(SecureString),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata(new SecureString(),FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AttachedPropertyValueChanged)
        );

        private static readonly DependencyProperty _passwordBindingMarshallerProperty = DependencyProperty.RegisterAttached(
            "PasswordBindingMarshaller",
            typeof(PasswordBindingMarshaller),
            typeof(PasswordBoxHelper),
            new PropertyMetadata()
        );

        public static void SetSecurePassword(PasswordBox element, SecureString secureString)
        {
            element.SetValue(SecurePasswordBindingProperty, secureString);
        }

        public static SecureString GetSecurePassword(PasswordBox element)
        {
            return element.GetValue(SecurePasswordBindingProperty) as SecureString;
        }

        private static void AttachedPropertyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // we'll need to hook up to one of the element's events
            // in order to allow the GC to collect the control, we'll wrap the event handler inside an object living in an attached property
            // don't be tempted to use the Unloaded event as that will be fired  even when the control is still alive and well (e.g. switching tabs in a tab control) 
            var passwordBox = (PasswordBox)d;
            var bindingMarshaller = passwordBox.GetValue(_passwordBindingMarshallerProperty) as PasswordBindingMarshaller;
            if (bindingMarshaller == null)
            {
                bindingMarshaller = new PasswordBindingMarshaller(passwordBox);
                passwordBox.SetValue(_passwordBindingMarshallerProperty, bindingMarshaller);
            }

            bindingMarshaller.UpdatePasswordBox(e.NewValue as SecureString);
        }

        private sealed class PasswordBindingMarshaller
        {
            private readonly PasswordBox passwordBox;
            private bool isMarshalling;

            public PasswordBindingMarshaller(PasswordBox passwordBox)
            {
                this.passwordBox = passwordBox;
                this.passwordBox.PasswordChanged += this.PasswordBoxPasswordChanged;
            }

            public void UpdatePasswordBox(SecureString newPassword)
            {
                if (isMarshalling)
                {
                    return;
                }

                isMarshalling = true;
                try
                {
                    // setting up the SecuredPassword won't trigger a visual update so we'll have to use the Password property
                    passwordBox.Password = newPassword?.ToUnsecuredString() ?? string.Empty;

                    // you may try the statement below, however the benefits are minimal security wise (you still have to extract the unsecured password for copying)
                    //newPassword.CopyInto(_passwordBox.SecurePassword);
                }
                finally
                {
                    isMarshalling = false;
                }
            }

            private void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
            {
                if (isMarshalling)
                {
                    return;
                }

                isMarshalling = true;
                try
                {
                    SetSecurePassword(passwordBox, passwordBox.SecurePassword.Copy());
                }
                finally
                {
                    isMarshalling = false;
                }
            }
        }
    }
}