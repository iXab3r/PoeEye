using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeEye.Utilities {
    public sealed class InvokeCommandActionDp : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(InvokeCommandActionDp), (PropertyMetadata)null);
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(InvokeCommandActionDp), (PropertyMetadata)null);
        private string commandName;

        public string CommandName
        {
            get
            {
                this.ReadPreamble();
                return this.commandName;
            }
            set
            {
                if (!(this.CommandName != value))
                    return;
                this.WritePreamble();
                this.commandName = value;
                this.WritePostscript();
            }
        }

        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(InvokeCommandActionDp.CommandProperty);
            }
            set
            {
                this.SetValue(InvokeCommandActionDp.CommandProperty, (object)value);
            }
        }

        public object CommandParameter
        {
            get
            {
                return this.GetValue(InvokeCommandActionDp.CommandParameterProperty);
            }
            set
            {
                this.SetValue(InvokeCommandActionDp.CommandParameterProperty, value);
            }
        }

        protected override void Invoke(object parameter)
        {
            if (this.AssociatedObject == null)
                return;
            ICommand command = this.ResolveCommand();
            if (command == null || !command.CanExecute(this.CommandParameter))
                return;
            command.Execute(this.CommandParameter);
        }

        private ICommand ResolveCommand()
        {
            ICommand command = (ICommand)null;
            if (this.Command != null)
                command = this.Command;
            else if (this.AssociatedObject != null)
            {
                foreach (PropertyInfo property in this.AssociatedObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (typeof(ICommand).IsAssignableFrom(property.PropertyType) && string.Equals(property.Name, this.CommandName, StringComparison.Ordinal))
                        command = (ICommand)property.GetValue((object)this.AssociatedObject, (object[])null);
                }
            }
            return command;
        }
    }
}