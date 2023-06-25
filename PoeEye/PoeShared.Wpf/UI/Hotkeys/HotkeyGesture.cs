using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using PInvoke;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public sealed record HotkeyGesture
{
    public static readonly HotkeyGesture Empty = new();
    public const char ModifiersDelimiter = '+';

    private static readonly IDictionary<Key, string> KnownSpecialKeys = new Dictionary<Key, string>();

    static HotkeyGesture()
    {
        KnownSpecialKeys[Key.OemPlus] = "+";
        KnownSpecialKeys[Key.OemMinus] = "-";
        KnownSpecialKeys[Key.OemTilde] = "`";
        KnownSpecialKeys[Key.Divide] = "/";
        KnownSpecialKeys[Key.Add] = "Num +";
        KnownSpecialKeys[Key.Multiply] = "Num *";
        KnownSpecialKeys[Key.Subtract] = "Num -";
    }

    public HotkeyGesture()
    {
    }

    /// <summary>
    /// Creates a new HotkeyGesture with the given keyboard key and optional modifier keys.
    /// </summary>
    /// <param name="key">The keyboard key of the hotkey gesture.</param>
    /// <param name="modifierKeys">The optional modifier keys of the hotkey gesture.</param>
    /// <remarks>
    /// If the key is None and modifier keys are specified, the key is set to the appropriate default key
    /// depending on the modifier key (Control, Alt, Shift, or Windows).
    /// </remarks>
    public HotkeyGesture(Key key, ModifierKeys modifierKeys = ModifierKeys.None) : this()
    {
        if (key == Key.None && modifierKeys != ModifierKeys.None)
        {
            key = modifierKeys switch
            {
                ModifierKeys.Alt => Key.LeftAlt,
                ModifierKeys.Control => Key.LeftCtrl,
                ModifierKeys.Shift => Key.LeftShift,
                ModifierKeys.Windows => Key.LWin,
                _ => Key.None
            };
        }

        switch (key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                modifierKeys &= ~ModifierKeys.Control;
                break;
            case Key.LeftAlt:
            case Key.RightAlt:
                modifierKeys &= ~ModifierKeys.Alt;
                break;
            case Key.LeftShift:
            case Key.RightShift:
                modifierKeys &= ~ModifierKeys.Shift;
                break;
            case Key.LWin:
            case Key.RWin:
                modifierKeys &= ~ModifierKeys.Windows;
                break;
        }

        Key = key;
        ModifierKeys = modifierKeys;
    }

    /// <summary>
    /// Creates a new HotkeyGesture with the given mouse button and optional modifier keys.
    /// </summary>
    /// <param name="mouseButton">The mouse button of the hotkey gesture.</param>
    /// <param name="modifierKeys">The optional modifier keys of the hotkey gesture.</param>
    public HotkeyGesture(MouseButton mouseButton, ModifierKeys modifierKeys = ModifierKeys.None) : this()
    {
        MouseButton = mouseButton;
        ModifierKeys = modifierKeys;
    }

    /// <summary>
    /// Creates a new HotkeyGesture based on an existing one, with optional modifier keys.
    /// </summary>
    /// <param name="keys">The existing hotkey gesture to base the new one on.</param>
    /// <param name="modifierKeys">The optional modifier keys of the hotkey gesture.</param>
    public HotkeyGesture(HotkeyGesture keys, ModifierKeys modifierKeys = ModifierKeys.None) : this()
    {
        MouseButton = keys.MouseButton;
        Key = keys.Key;
        ModifierKeys = modifierKeys;
    }

    /// <summary>
    /// Creates a new HotkeyGesture with the given mouse button and optional modifier keys.
    /// </summary>
    /// <param name="mouseButton">The mouse button of the hotkey gesture.</param>
    /// <param name="modifierKeys">The optional modifier keys of the hotkey gesture.</param>
    public HotkeyGesture(MouseButtons mouseButton, ModifierKeys modifierKeys = ModifierKeys.None) : this()
    {
        var button = default(MouseButton?);
        switch (mouseButton)
        {
            case MouseButtons.Left:
                button = System.Windows.Input.MouseButton.Left;
                break;
            case MouseButtons.Right:
                button = System.Windows.Input.MouseButton.Right;
                break;
            case MouseButtons.Middle:
                button = System.Windows.Input.MouseButton.Middle;
                break;
            case MouseButtons.XButton1:
                button = System.Windows.Input.MouseButton.XButton1;
                break;
            case MouseButtons.XButton2:
                button = System.Windows.Input.MouseButton.XButton2;
                break;
        }

        MouseButton = button;
        ModifierKeys = modifierKeys;
    }
    
    /// <summary>
    /// Creates a new HotkeyGesture with the given mouse wheel action and optional modifier keys.
    /// </summary>
    /// <param name="mouseWheel">The mouse wheel action of the hotkey gesture.</param>
    /// <param name="modifierKeys">The optional modifier keys of the hotkey gesture.</param>
    public HotkeyGesture(MouseWheelAction mouseWheel, ModifierKeys modifierKeys = ModifierKeys.None) : this()
    {
        ModifierKeys = modifierKeys;
        MouseWheel = mouseWheel;
    }

    /// <summary>
    /// Represents the mouse button used in the gesture.
    /// </summary>
    public MouseButton? MouseButton { get; init; }

    /// <summary>
    /// Represents the keyboard key used in the gesture.
    /// </summary>
    public Key Key { get; init; }

    /// <summary>
    /// Represents the modifier keys used in the gesture.
    /// </summary>
    public ModifierKeys ModifierKeys { get; init; }

    /// <summary>
    /// Represents the mouse wheel action in the gesture.
    /// </summary>
    public MouseWheelAction MouseWheel { get; init; }

    /// <summary>
    /// Indicates if the gesture is a keyboard gesture.
    /// </summary>
    public bool IsKeyboard => Key != Key.None;

    /// <summary>
    /// Indicates if the gesture includes a mouse button.
    /// </summary>
    public bool IsMouseButton => MouseButton != null;

    /// <summary>
    /// Indicates if the gesture includes a mouse wheel action.
    /// </summary>
    public bool IsMouseWheel => MouseWheel != MouseWheelAction.None;

    /// <summary>
    /// Indicates if the gesture is a mouse gesture.
    /// </summary>
    public bool IsMouse => IsMouseButton || IsMouseWheel;

    /// <summary>
    /// Indicates if the gesture is empty.
    /// </summary>
    public bool IsEmpty => MouseButton == null && Key == Key.None && ModifierKeys == ModifierKeys.None && MouseWheel == MouseWheelAction.None;


    /// <summary>
    /// Compares this gesture with another for equality, optionally ignoring modifiers.
    /// </summary>
    /// <param name="other">The other HotkeyGesture to compare with.</param>
    /// <param name="ignoreModifiers">Whether to ignore modifier keys in the comparison.</param>
    /// <returns>True if the gestures are equal; false otherwise.</returns>
    public bool Equals(HotkeyGesture other, bool ignoreModifiers)
    {
        if (!ignoreModifiers)
        {
            return Equals(other);
        }

        return HotkeysAreEqual(this, other, true);
    }

    private static bool HotkeysAreEqual(HotkeyGesture key1, HotkeyGesture key2, bool ignoreModifiers)
    {
        return !ignoreModifiers ? key1.ToString().Equals(key2.ToString()) : ExtractKeyWithoutModifiers(key1).Equals(ExtractKeyWithoutModifiers(key2));
    }

    private static string ExtractKeyWithoutModifiers(HotkeyGesture key)
    {
        if (key.MouseButton != null)
        {
            return key.MouseButton.ToString();
        }
        else if (key.Key != Key.None)
        {
            return key.Key.ToString();
        }
        else if (key.MouseWheel != MouseWheelAction.None)
        {
            return key.MouseWheel.ToString();
        }

        return string.Empty;
    }

    public override string ToString()
    {
        var keys = new List<string>();
        if ((ModifierKeys & ModifierKeys.Control) == ModifierKeys.Control)
        {
            GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_CONTROL).AddTo(keys);
        }

        if ((ModifierKeys & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_MENU).AddTo(keys);
        }

        if ((ModifierKeys & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            GetLocalizedKeyStringUnsafe(User32.VirtualKey.VK_SHIFT).AddTo(keys);
        }

        if ((ModifierKeys & ModifierKeys.Windows) == ModifierKeys.Windows)
        {
            "Windows".AddTo(keys);
        }

        if (KnownSpecialKeys.TryGetValue(Key, out var specialKey))
        {
            specialKey.AddTo(keys);
        }
        else if (Key != Key.None)
        {
            GetLocalizedKeyString(Key).AddTo(keys);
        }

        if (MouseButton != null)
        {
            $"Mouse{MouseButton}".AddTo(keys);
        }

        if (MouseWheel != MouseWheelAction.None)
        {
            $"{MouseWheel}".AddTo(keys);
        }

        if (keys.Count == 0)
        {
            return "None";
        }

        return string.Join("+", keys);
    }

    private static string GetLocalizedKeyString(Key key)
    {
        if (key >= Key.BrowserBack)
        {
            return key.ToString();
        }

        var virtualKey = (User32.VirtualKey) KeyInterop.VirtualKeyFromKey(key);
        return GetLocalizedKeyStringUnsafe(virtualKey) ?? key.ToString();
    }

    private static string GetLocalizedKeyStringUnsafe(User32.VirtualKey key)
    {
        // strip any modifier keys
        var keyCode = (int) key & 0xffff;

        var sb = new StringBuilder(256);
        var scanCode = User32.MapVirtualKey(keyCode, User32.MapVirtualKeyTranslation.MAPVK_VK_TO_VSC);

        // shift the scan code to the high word
        scanCode = scanCode << 16;
        if (keyCode == 45 ||
            keyCode == 46 ||
            keyCode == 144 ||
            33 <= keyCode && keyCode <= 40)
        {
            // add the extended key flag
            scanCode |= 0x1000000;
        }

        var resultLength = UnsafeNative.GetKeyNameText(scanCode, sb, 256);
        return resultLength > 0 ? sb.ToString() : null;
    }
}