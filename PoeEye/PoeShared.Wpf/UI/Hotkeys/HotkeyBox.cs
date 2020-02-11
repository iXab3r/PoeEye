using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Native;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace PoeShared.UI.Hotkeys
{
    [TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
    public class HotKeyBox : Control
    {
        private const string PART_TextBox = "PART_TextBox";
        public static readonly ISet<Key> EscapeKeys = new HashSet<Key> { Key.Escape, Key.Back };

        public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
            "HotKey",
            typeof(HotkeyGesture),
            typeof(HotKeyBox),
            new FrameworkPropertyMetadata(default(HotkeyGesture), OnHotKeyChanged) {BindsTwoWayByDefault = true});

        public static readonly DependencyProperty AreModifierKeysRequiredProperty = DependencyProperty.Register(
            "AreModifierKeysRequired",
            typeof(bool),
            typeof(HotKeyBox),
            new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            "Watermark",
            typeof(string),
            typeof(HotKeyBox),
            new PropertyMetadata(default(string)));

        private static readonly DependencyPropertyKey TextPropertyKey = DependencyProperty.RegisterReadOnly(
            "Text",
            typeof(string),
            typeof(HotKeyBox),
            new PropertyMetadata(default(string)));

        public static readonly DependencyProperty AcceptsMouseKeysProperty = DependencyProperty.Register(
            "AcceptsMouseKeys",
            typeof(bool),
            typeof(HotKeyBox),
            new PropertyMetadata(false));
        
        /// <summary>Identifies the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsReturn" /> dependency property. </summary>
        /// <returns>The identifier for the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsReturn" /> dependency property.</returns>
        public static readonly DependencyProperty AcceptsReturnProperty = KeyboardNavigation.AcceptsReturnProperty.AddOwner(typeof (HotKeyBox));
        /// <summary>Identifies the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsTab" /> dependency property. </summary>
        /// <returns>The identifier for the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsTab" /> dependency property.</returns>
        public static readonly DependencyProperty AcceptsTabProperty = DependencyProperty.Register(nameof (TextBoxBase.AcceptsTab), typeof (bool), typeof (HotKeyBox), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty TextProperty = TextPropertyKey.DependencyProperty;

        private TextBox textBox;

        static HotKeyBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HotKeyBox), new FrameworkPropertyMetadata(typeof(HotKeyBox)));
            EventManager.RegisterClassHandler(typeof(HotKeyBox), GotFocusEvent, new RoutedEventHandler(OnGotFocus));
        }
        
        public bool AcceptsReturn
        {
            get => (bool) this.GetValue(AcceptsReturnProperty);
            set => this.SetValue(AcceptsReturnProperty, value);
        }
        
        public bool AcceptsTab
        {
            get => (bool) this.GetValue(AcceptsTabProperty);
            set => this.SetValue(AcceptsTabProperty, value);
        }

        public bool AcceptsMouseKeys
        {
            get => (bool) GetValue(AcceptsMouseKeysProperty);
            set => SetValue(AcceptsMouseKeysProperty, value);
        }

        public HotkeyGesture HotKey
        {
            get => (HotkeyGesture) GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        public bool AreModifierKeysRequired
        {
            get => (bool) GetValue(AreModifierKeysRequiredProperty);
            set => SetValue(AreModifierKeysRequiredProperty, value);
        }

        public string Watermark
        {
            get => (string) GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            private set => SetValue(TextPropertyKey, value);
        }

        private static void OnHotKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (HotKeyBox) d;
            ctrl.UpdateText();
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            var hotKeyBox = (HotKeyBox) sender;

            // If we're an editable HotKeyBox, forward focus to the TextBox or previous element
            if (e.Handled)
            {
                return;
            }

            if (!hotKeyBox.Focusable || hotKeyBox.textBox == null)
            {
                return;
            }

            if (!Equals(e.OriginalSource, hotKeyBox))
            {
                return;
            }

            // MoveFocus takes a TraversalRequest as its argument.
            var request = new TraversalRequest(
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift
                    ? FocusNavigationDirection.Previous
                    : FocusNavigationDirection.Next);
            // Gets the element with keyboard focus.
            var elementWithFocus = Keyboard.FocusedElement as UIElement;
            // Change keyboard focus.
            elementWithFocus?.MoveFocus(request);
            e.Handled = true;
        }

        public override void OnApplyTemplate()
        {
            if (textBox != null)
            {
                textBox.PreviewMouseDown -= TextBoxOnPreviewMouseDown;
                textBox.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
                textBox.GotFocus -= TextBoxOnGotFocus;
                textBox.LostFocus -= TextBoxOnLostFocus;
                textBox.TextChanged -= TextBoxOnTextChanged;
            }

            base.OnApplyTemplate();

            textBox = Template.FindName(PART_TextBox, this) as TextBox;
            if (textBox == null)
            {
                return;
            }

            textBox.PreviewKeyDown += TextBoxOnPreviewKeyDown;
            textBox.PreviewMouseDown += TextBoxOnPreviewMouseDown;

            textBox.GotFocus += TextBoxOnGotFocus;
            textBox.LostFocus += TextBoxOnLostFocus;
            textBox.TextChanged += TextBoxOnTextChanged;
            UpdateText();
        }

        private void TextBoxOnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!AcceptsMouseKeys)
            {
                return;
            }
            var currentModifierKeys = GetCurrentModifierKeys();
            if (e.XButton1 == MouseButtonState.Pressed)
            {
                HotKey = new HotkeyGesture(MouseButton.XButton1, currentModifierKeys);
            }
            else if (e.XButton2 == MouseButtonState.Pressed)
            {
                HotKey = new HotkeyGesture(MouseButton.XButton2, currentModifierKeys);
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                HotKey = new HotkeyGesture(MouseButton.Middle, currentModifierKeys);
            }
        }

        private void TextBoxOnTextChanged(object sender, TextChangedEventArgs args)
        {
            textBox.SelectionStart = textBox.Text.Length;
        }

        private void TextBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcherOnThreadPreprocessMessage;
        }

        private void ComponentDispatcherOnThreadPreprocessMessage(ref MSG msgRaw, ref bool handled)
        {
            var msg = (User32.WindowMessage) msgRaw.message;
            if (msg == User32.WindowMessage.WM_HOTKEY)
            {
                handled = true;
            }
        }

        private void TextBoxOnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcherOnThreadPreprocessMessage;
        }

        private void TextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == Key.System
                ? e.SystemKey
                : e.Key;
            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.RWin:
                case Key.LWin:
                case { } keyTab when keyTab == Key.Tab && !AcceptsTab:
                case { } keyReturn when keyReturn == Key.Return && !AcceptsReturn:
                    return;
            }

            e.Handled = true;

            var currentModifierKeys = GetCurrentModifierKeys();
            if (currentModifierKeys == ModifierKeys.None && EscapeKeys.Contains(key))
            {
                HotKey = null;
            }
            else if (currentModifierKeys != ModifierKeys.None || !AreModifierKeysRequired)
            {
                HotKey = new HotkeyGesture(key, currentModifierKeys);
            }

            UpdateText();
        }

        private static ModifierKeys GetCurrentModifierKeys()
        {
            var modifier = ModifierKeys.None;
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                modifier |= ModifierKeys.Control;
            }

            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                modifier |= ModifierKeys.Alt;
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                modifier |= ModifierKeys.Shift;
            }

            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            {
                modifier |= ModifierKeys.Windows;
            }

            return modifier;
        }

        private void UpdateText()
        {
            var hotkey = HotKey ?? new HotkeyGesture(Key.None);
            Text = hotkey.ToString();
        }
    }
}