using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PoeEye.Utilities
{
    public sealed class InvokeCommandActionDp : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(InvokeCommandActionDp), null);

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(InvokeCommandActionDp), null);

        private string commandName;

        public string CommandName
        {
            get
            {
                ReadPreamble();
                return commandName;
            }
            set
            {
                if (!(CommandName != value))
                {
                    return;
                }

                WritePreamble();
                commandName = value;
                WritePostscript();
            }
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void Invoke(object parameter)
        {
            if (AssociatedObject == null)
            {
                return;
            }

            var command = ResolveCommand();
            if (command == null || !command.CanExecute(CommandParameter))
            {
                return;
            }

            command.Execute(CommandParameter);
        }

        private ICommand ResolveCommand()
        {
            var command = (ICommand)null;
            if (Command != null)
            {
                command = Command;
            }
            else if (AssociatedObject != null)
            {
                foreach (var property in AssociatedObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (typeof(ICommand).IsAssignableFrom(property.PropertyType) && string.Equals(property.Name, CommandName, StringComparison.Ordinal))
                    {
                        command = (ICommand)property.GetValue(AssociatedObject, null);
                    }
                }
            }

            return command;
        }
    }
}