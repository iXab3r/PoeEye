using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ICSharpCode.AvalonEdit;
using JetBrains.Annotations;
using static System.String;

namespace PoeEye.TradeMonitor.Controls
{
    /// <summary>
    ///     Class that inherits from the AvalonEdit TextEditor control to
    ///     enable MVVM interaction.
    /// </summary>
    public class ExtendedTextEditor : TextEditor, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TextContentProperty = DependencyProperty.Register(
            "TextContent", typeof(string), typeof(ExtendedTextEditor), new PropertyMetadata("", OnMyContentChanged));

        public string TextContent
        {
            get { return Text; }
            set { Text = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private static void OnMyContentChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (ExtendedTextEditor) sender;
            var newValue = e.NewValue as string;
            if (!string.Equals(newValue, control.TextContent, StringComparison.Ordinal))
            {
                control.TextContent = newValue;
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            SetCurrentValue(TextContentProperty, Text);
            base.OnTextChanged(e);
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}