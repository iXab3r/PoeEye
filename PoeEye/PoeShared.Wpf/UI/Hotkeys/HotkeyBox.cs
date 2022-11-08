using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using PInvoke;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PoeShared.Themes;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace PoeShared.UI;

[TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
public class HotKeyBox : Control
{
    public const string PART_TextBox = "PART_TextBox";
    private static readonly ISet<Key> EscapeKeys = new HashSet<Key> {Key.Escape, Key.Back, Key.Delete};

    public static readonly DependencyProperty HotKeyProperty = DependencyProperty.Register(
        "HotKey",
        typeof(HotkeyGesture),
        typeof(HotKeyBox),
        new FrameworkPropertyMetadata(default(HotkeyGesture)) {BindsTwoWayByDefault = true});

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

    public static readonly DependencyProperty AcceptsModifiersProperty = DependencyProperty.Register(
        "AcceptsModifiers", typeof(bool), typeof(HotKeyBox), new PropertyMetadata(true));

    public static readonly DependencyProperty AcceptsMouseWheelProperty = DependencyProperty.Register(
        "AcceptsMouseWheel", typeof(bool), typeof(HotKeyBox), new PropertyMetadata(default(bool)));

    /// <summary>Identifies the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsReturn" /> dependency property. </summary>
    /// <returns>The identifier for the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsReturn" /> dependency property.</returns>
    public static readonly DependencyProperty AcceptsReturnProperty = KeyboardNavigation.AcceptsReturnProperty.AddOwner(typeof(HotKeyBox));

    /// <summary>Identifies the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsTab" /> dependency property. </summary>
    /// <returns>The identifier for the <see cref="P:System.Windows.Controls.Primitives.TextBoxBase.AcceptsTab" /> dependency property.</returns>
    public static readonly DependencyProperty AcceptsTabProperty = DependencyProperty.Register(nameof(TextBoxBase.AcceptsTab), typeof(bool), typeof(HotKeyBox), new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsInEditModeProperty = DependencyProperty.Register(nameof(IsInEditMode), typeof(bool), typeof(HotKeyBox), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty TextProperty = TextPropertyKey.DependencyProperty;

    private readonly CompositeDisposable anchors = new();

    private HotkeyGesture lastKeyDown;
    private DateTime lastKeyDownTimestamp;
    private TextBox textBox;

    static HotKeyBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HotKeyBox), new FrameworkPropertyMetadata(typeof(HotKeyBox)));
        EventManager.RegisterClassHandler(typeof(HotKeyBox), GotFocusEvent, new RoutedEventHandler(OnGotFocus));
    }

    public bool AcceptsModifiers
    {
        get => (bool) GetValue(AcceptsModifiersProperty);
        set => SetValue(AcceptsModifiersProperty, value);
    }

    public bool AcceptsMouseWheel
    {
        get => (bool) GetValue(AcceptsMouseWheelProperty);
        set => SetValue(AcceptsMouseWheelProperty, value);
    }

    public bool AcceptsReturn
    {
        get => (bool) GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public bool AcceptsTab
    {
        get => (bool) GetValue(AcceptsTabProperty);
        set => SetValue(AcceptsTabProperty, value);
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

    public bool IsInEditMode
    {
        get { return (bool) GetValue(IsInEditModeProperty); }
        set { SetValue(IsInEditModeProperty, value); }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        textBox = Template.FindName(PART_TextBox, this) as TextBox;
        if (textBox == null)
        {
            return;
        }

        var keyboardEvents = Observable.Using(() =>
        {
            textBox.PreviewKeyDown += TextBoxOnPreviewKeyDown;
            textBox.GotFocus += TextBoxOnGotFocus;
            textBox.LostFocus += TextBoxOnLostFocus;
            textBox.TextChanged += TextBoxOnTextChanged;
            return Disposable.Create(() =>
            {
                textBox.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
                textBox.GotFocus -= TextBoxOnGotFocus;
                textBox.LostFocus -= TextBoxOnLostFocus;
                textBox.TextChanged -= TextBoxOnTextChanged;
            });
        }, disposable => Observable.Never<Unit>());
        
        var mouseEvents = Observable.Using(() =>
        {
            textBox.PreviewMouseDown += TextBoxOnPreviewMouseDown;
            this.PreviewMouseWheel += TextBoxOnPreviewMouseWheel;
            return Disposable.Create(() =>
            {
                textBox.PreviewMouseDown -= TextBoxOnPreviewMouseDown;
                this.PreviewMouseWheel -= TextBoxOnPreviewMouseWheel;
            });
        }, disposable => Observable.Never<Unit>());
        
        this.Observe(IsInEditModeProperty, x => x.IsInEditMode)
            .Select(x => x && (AcceptsMouseWheel || AcceptsMouseKeys) ? mouseEvents : Observable.Empty<Unit>())
            .Switch()
            .Subscribe()
            .AddTo(anchors);

        this.Observe(IsInEditModeProperty, x => x.IsInEditMode)
            .Select(x => x ? keyboardEvents : Observable.Empty<Unit>())
            .Switch()
            .Subscribe()
            .AddTo(anchors);

        var focusLost = this.Observe(IsKeyboardFocusWithinProperty, x => x.IsKeyboardFocusWithin)
            .Where(x => x == false);
        
        this.Observe(IsInEditModeProperty, x => x.IsInEditMode)
            .Select(x => x ? focusLost : Observable.Empty<bool>())
            .Switch()
            .Subscribe(_ => IsInEditMode = false)
            .AddTo(anchors);

        var hotkeySource = this.Observe(HotKeyProperty, x => x.HotKey)
            .Select(x => x ?? HotkeyGesture.Empty)
            .Select(x => x.ToString());
        
        this.Observe(IsInEditModeProperty, x => x.IsInEditMode)
            .Select(x => x ? Observable.Return(PrepareTooltip()) : hotkeySource)
            .Switch()
            .Subscribe(x => Text = x)
            .AddTo(anchors);
    }

    private string PrepareTooltip()
    {
        var result = new List<string> {AwesomeIcons.Keyboard};
        if (AcceptsMouseKeys || AcceptsMouseWheel)
        {
            result.Add(AwesomeIcons.MousePointer);
        }
        result.Add(" Press...");
        return result.JoinStrings(" ");
    }

    private void TextBoxOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!IsInEditMode)
        {
            throw new InvalidStateException("HotkeyBox is not in EditMode");
        }
        
        if (!AcceptsMouseWheel)
        {
            return;
        }

        if (!TryGetModifiers(out var currentModifierKeys))
        {
            return;
        }

        if (e.Delta != 0)
        {
            HotKey = new HotkeyGesture(e.Delta > 0 ? MouseWheelAction.WheelUp : MouseWheelAction.WheelDown, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
        }
    }

    private void TextBoxOnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsInEditMode)
        {
            throw new InvalidStateException("HotkeyBox is not in EditMode");
        }
        
        if (!AcceptsMouseKeys)
        {
            return;
        }

        if (!TryGetModifiers(out var currentModifierKeys))
        {
            return;
        }

        if (e.XButton1 == MouseButtonState.Pressed)
        {
            HotKey = new HotkeyGesture(MouseButton.XButton1, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
        }
        else if (e.XButton2 == MouseButtonState.Pressed)
        {
            HotKey = new HotkeyGesture(MouseButton.XButton2, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
        }
        else if (e.MiddleButton == MouseButtonState.Pressed)
        {
            HotKey = new HotkeyGesture(MouseButton.Middle, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            HotKey = new HotkeyGesture(MouseButton.Left, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            HotKey = new HotkeyGesture(MouseButton.Right, currentModifierKeys);
            e.Handled = true;
            IsInEditMode = false;
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
        if (!IsInEditMode)
        {
            throw new InvalidStateException("HotkeyBox is not in EditMode");
        }
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
            case { } keyTab when keyTab == Key.Tab && !AcceptsTab:
            case { } keyReturn when keyReturn == Key.Return && !AcceptsReturn:
                return;
        }

        e.Handled = true;

        if (!TryGetModifiers(out var currentModifierKeys))
        {
            return;
        }

        var currentHotKey = new HotkeyGesture(key, currentModifierKeys);

        var isClearHotKey = EscapeKeys.Contains(key) && currentModifierKeys == ModifierKeys.None;
        var isDoubleClick = DateTime.Now - lastKeyDownTimestamp < TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime) && currentHotKey.Equals(lastKeyDown);
        if (isClearHotKey && !isDoubleClick)
        {
            HotKey = null;
            IsInEditMode = false;
        }
        else if (currentModifierKeys != ModifierKeys.None || !AreModifierKeysRequired)
        {
            HotKey = currentHotKey;
            IsInEditMode = false;
        }

        lastKeyDown = currentHotKey;
        lastKeyDownTimestamp = DateTime.Now;
    }

    private bool TryGetModifiers(out ModifierKeys modifierKeys)
    {
        var currentModifierKeys = UnsafeNative.GetCurrentModifierKeys();

        if (AcceptsModifiers)
        {
            modifierKeys = currentModifierKeys;
            return true;
        }

        modifierKeys = ModifierKeys.None;
        return currentModifierKeys == ModifierKeys.None;
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
}