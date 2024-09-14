using System;
using System.Windows.Input;
using Microsoft.AspNetCore.Components.Web;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using KeyboardEventArgs = Microsoft.AspNetCore.Components.Web.KeyboardEventArgs;
using MouseEventArgs = Microsoft.AspNetCore.Components.Web.MouseEventArgs;

namespace PoeShared.Blazor.Wpf.Services;

public sealed class WebKeyToWpfKeyConverter : LazyReactiveObject<WebKeyToWpfKeyConverter>, IConverter<KeyboardEventArgs, HotkeyGesture>
{
    private static readonly IFluentLog Log = typeof(WebKeyToWpfKeyConverter).PrepareLogger();

    public HotkeyGesture Convert(WheelEventArgs args)
    {
        var modifiers = FromJsEvent(args);
        var wheel = args.DeltaY switch
        {
            < 0 => MouseWheelAction.WheelUp,
            > 0 => MouseWheelAction.WheelDown,
            _ => MouseWheelAction.None
        };
        var gesture = new HotkeyGesture(wheel, modifiers);
        return gesture;
    }
    
    public HotkeyGesture Convert(MouseEventArgs args)
    {
        var modifiers = FromJsEvent(args);
        var button = FromJsButton(args.Button);
        var gesture = new HotkeyGesture(button, modifiers);
        return gesture;
    }
    
    public HotkeyGesture Convert(KeyboardEventArgs args)
    {
        var modifiers = ModifierKeys.None;
        if (args.CtrlKey)
        {
            modifiers |= ModifierKeys.Control;
        }

        if (args.AltKey)
        {
            modifiers |= ModifierKeys.Alt;
        }

        if (args.ShiftKey)
        {
            modifiers |= ModifierKeys.Shift;
        }

        if (args.MetaKey)
        {
            modifiers |= ModifierKeys.Windows;
        }

        var key = FromJsKeyCode(args.Code);
        if (key is Key.LeftCtrl or Key.RightCtrl && modifiers == ModifierKeys.Control)
        {
            modifiers = ModifierKeys.None;
        }

        if (key is Key.LeftShift or Key.RightShift && modifiers == ModifierKeys.Shift)
        {
            modifiers = ModifierKeys.None;
        }

        if (key is Key.LeftAlt or Key.RightAlt && modifiers == ModifierKeys.Alt)
        {
            modifiers = ModifierKeys.None;
        }

        if (key is Key.LWin or Key.RWin && modifiers == ModifierKeys.Windows)
        {
            modifiers = ModifierKeys.None;
        }

        var gesture = new HotkeyGesture(key, modifiers);
        return gesture;
    }

    private static ModifierKeys FromJsEvent(MouseEventArgs args)
    {
        var modifiers = ModifierKeys.None;
        if (args.CtrlKey)
        {
            modifiers |= ModifierKeys.Control;
        }

        if (args.AltKey)
        {
            modifiers |= ModifierKeys.Alt;
        }

        if (args.ShiftKey)
        {
            modifiers |= ModifierKeys.Shift;
        }

        if (args.MetaKey)
        {
            modifiers |= ModifierKeys.Windows;
        }

        return modifiers;
    }

    private static MouseButton FromJsButton(long button)
    {
        return button switch
        {
            0 => MouseButton.Left,   // Left button
            1 => MouseButton.Middle, // Middle button
            2 => MouseButton.Right,  // Right button
            3 => MouseButton.XButton1, // Additional button (back)
            4 => MouseButton.XButton2, // Additional button (forward)
            _ => throw new ArgumentOutOfRangeException(nameof(button), $"Unknown button value: {button}")
        };
    }

    private static Key FromJsKeyCode(string jsKeyCode)
    {
        var result = jsKeyCode switch
        {
            // Alphabet keys
            "KeyA" => Key.A,
            "KeyB" => Key.B,
            "KeyC" => Key.C,
            "KeyD" => Key.D,
            "KeyE" => Key.E,
            "KeyF" => Key.F,
            "KeyG" => Key.G,
            "KeyH" => Key.H,
            "KeyI" => Key.I,
            "KeyJ" => Key.J,
            "KeyK" => Key.K,
            "KeyL" => Key.L,
            "KeyM" => Key.M,
            "KeyN" => Key.N,
            "KeyO" => Key.O,
            "KeyP" => Key.P,
            "KeyQ" => Key.Q,
            "KeyR" => Key.R,
            "KeyS" => Key.S,
            "KeyT" => Key.T,
            "KeyU" => Key.U,
            "KeyV" => Key.V,
            "KeyW" => Key.W,
            "KeyX" => Key.X,
            "KeyY" => Key.Y,
            "KeyZ" => Key.Z,

            // Number row keys
            "Digit1" => Key.D1,
            "Digit2" => Key.D2,
            "Digit3" => Key.D3,
            "Digit4" => Key.D4,
            "Digit5" => Key.D5,
            "Digit6" => Key.D6,
            "Digit7" => Key.D7,
            "Digit8" => Key.D8,
            "Digit9" => Key.D9,
            "Digit0" => Key.D0,

            // Function keys
            "F1" => Key.F1,
            "F2" => Key.F2,
            "F3" => Key.F3,
            "F4" => Key.F4,
            "F5" => Key.F5,
            "F6" => Key.F6,
            "F7" => Key.F7,
            "F8" => Key.F8,
            "F9" => Key.F9,
            "F10" => Key.F10,
            "F11" => Key.F11,
            "F12" => Key.F12,
            "F13" => Key.F13,
            "F14" => Key.F14,
            "F15" => Key.F15,
            "F16" => Key.F16,
            "F17" => Key.F17,
            "F18" => Key.F18,
            "F19" => Key.F19,
            "F20" => Key.F20,
            "F21" => Key.F21,
            "F22" => Key.F22,
            "F23" => Key.F23,
            "F24" => Key.F24,

            // Special keys
            "Escape" => Key.Escape,
            "Tab" => Key.Tab,
            "CapsLock" => Key.CapsLock,
            "ShiftLeft" => Key.LeftShift,
            "ShiftRight" => Key.RightShift,
            "ControlLeft" => Key.LeftCtrl,
            "ControlRight" => Key.RightCtrl,
            "AltLeft" => Key.LeftAlt,
            "AltRight" => Key.RightAlt,
            "Space" => Key.Space,
            "Enter" => Key.Enter,
            "Backspace" => Key.Back,
            "Delete" => Key.Delete,
            "Insert" => Key.Insert,
            "Home" => Key.Home,
            "End" => Key.End,
            "PageUp" => Key.PageUp,
            "PageDown" => Key.PageDown,
            "ArrowLeft" => Key.Left,
            "ArrowUp" => Key.Up,
            "ArrowRight" => Key.Right,
            "ArrowDown" => Key.Down,

            // Numpad keys
            "Numpad0" => Key.NumPad0,
            "Numpad1" => Key.NumPad1,
            "Numpad2" => Key.NumPad2,
            "Numpad3" => Key.NumPad3,
            "Numpad4" => Key.NumPad4,
            "Numpad5" => Key.NumPad5,
            "Numpad6" => Key.NumPad6,
            "Numpad7" => Key.NumPad7,
            "Numpad8" => Key.NumPad8,
            "Numpad9" => Key.NumPad9,
            "NumpadAdd" => Key.Add,
            "NumpadSubtract" => Key.Subtract,
            "NumpadMultiply" => Key.Multiply,
            "NumpadDivide" => Key.Divide,
            "NumpadDecimal" => Key.Decimal,
            "NumpadEnter" => Key.Enter,

            // Additional keys
            "Semicolon" => Key.Oem1, // May vary based on keyboard layout
            "Equal" => Key.OemPlus,
            "Comma" => Key.OemComma,
            "Minus" => Key.OemMinus,
            "Period" => Key.OemPeriod,
            "Slash" => Key.Oem2, // May vary based on keyboard layout
            "Backquote" => Key.Oem3, // May vary based on keyboard layout
            "BracketLeft" => Key.Oem4,
            "Backslash" => Key.Oem5,
            "BracketRight" => Key.Oem6,
            "Quote" => Key.Oem7,

            // Media keys (these might not be captured by some browsers)
            "MediaTrackNext" => Key.MediaNextTrack,
            "MediaTrackPrevious" => Key.MediaPreviousTrack,
            "MediaStop" => Key.MediaStop,
            "MediaPlayPause" => Key.MediaPlayPause,
            "AudioVolumeMute" => Key.VolumeMute,
            "AudioVolumeUp" => Key.VolumeUp,
            "AudioVolumeDown" => Key.VolumeDown,

            // Others
            "PrintScreen" => Key.PrintScreen,
            "ScrollLock" => Key.Scroll,
            "Pause" => Key.Pause,
            "ContextMenu" => Key.Apps,
            "NumLock" => Key.NumLock,

            var _ => Key.None
        };

        if (result == Key.None && !string.IsNullOrEmpty(jsKeyCode))
        {
            Log.Warn($"Unmapped key code: '{jsKeyCode}'");
        }

        return result;
    }
}