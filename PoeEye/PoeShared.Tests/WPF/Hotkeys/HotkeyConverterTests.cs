using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualBasic.Logging;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys;

[TestFixture]
public class HotkeyConverterTests : FixtureBase
{
    [Test]
    public void ShouldSerializeAllKeyboardToString([Values] Key key)
    {
        // Given
        var empty = new HotkeyGesture(Key.None).ToString();
        var hotkey = new HotkeyGesture(key, ModifierKeys.None);

        // When
        var result = hotkey.ToString();

        // Then
        if (key != Key.None)
        {
            result.ShouldNotBe(empty);
        }
    }

    [Test]
    public void ShouldConvertAllKeys([Values] Key key)
    {
        //Given
        //When
        var invariant = HotkeyMapping.KeysToInvariantName[key];
        var invariantKey = HotkeyMapping.InvariantNameToKey[invariant];

        //Then
        invariant.ShouldNotBeEmpty();
        invariantKey.ShouldBe(key);
    }
        
    [Test]
    public void ShouldSerializeAllMouseWheelStatesToString([Values] MouseWheelAction wheel)
    {
        // Given
        var empty = new HotkeyGesture().ToString();
        var hotkey = new HotkeyGesture(wheel);

        // When
        var result = hotkey.ToString();

        // Then
        if (wheel != MouseWheelAction.None)
        {
            result.ShouldNotBe(empty);
        }
    }
        
    [Test]
    public void ShouldConvertAllKeyboard([Values] Key key)
    {
        // Given
        var instance = CreateInstance();
        var hotkey = new HotkeyGesture(key);
        var hotkeyAsString = instance.ConvertToString(hotkey);

        // When
        var result = instance.ConvertFrom(hotkeyAsString);

        // Then
        result.ShouldNotBeNull();
        result.ShouldBeOfType<HotkeyGesture>();
        result.ShouldBe(hotkey);
    }

    [TestCase(Key.None, ModifierKeys.None, "None")]
    [TestCase(Key.OemPlus, ModifierKeys.None, "=")]
    [TestCase(Key.OemMinus, ModifierKeys.None, "-")]
    [TestCase(Key.Multiply, ModifierKeys.None, "Num *")]
    [TestCase(Key.Add, ModifierKeys.None, "Num +")]
    [TestCase(Key.Subtract, ModifierKeys.None, "Num -")]
    [TestCase(Key.Divide, ModifierKeys.None, "Num /")]
    [TestCase(Key.Decimal, ModifierKeys.None, "Decimal")]
    [TestCase(Key.Enter, ModifierKeys.None, "ENTER")]
    [TestCase(Key.Return, ModifierKeys.None, "ENTER")]
    [TestCase(Key.End, ModifierKeys.None, "End")]
    [TestCase(Key.D0, ModifierKeys.None, "0")]
    [TestCase(Key.D9, ModifierKeys.None, "9")]
    [TestCase(Key.A, ModifierKeys.None, "A")]
    [TestCase(Key.Z, ModifierKeys.None, "Z")]
    [TestCase(Key.Clear, ModifierKeys.None, "Clear")]
    [TestCase(Key.A, ModifierKeys.Control, "CONTROL+A")]
    [TestCase(Key.W, ModifierKeys.Shift, "SHIFT+W")]
    [TestCase(Key.A, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows, "ALT+CONTROL+SHIFT+WINDOWS+A")]
    public void ShouldSerializeKeyboard(Key key, ModifierKeys modifierKeys, string expected)
    {
        // Given
        var instance = CreateInstance();
        var hotkey = new HotkeyGesture(key, modifierKeys);

        // When
        var result = instance.ConvertToString(hotkey);

        // Then
        result.ShouldNotBeNull();
        result.ShouldBe(expected);
    }

    [TestCase(Key.None, ModifierKeys.None)]
    [TestCase(Key.OemPlus, ModifierKeys.None)]
    [TestCase(Key.OemMinus, ModifierKeys.None)]
    [TestCase(Key.Divide, ModifierKeys.None)]
    [TestCase(Key.Multiply, ModifierKeys.None)]
    [TestCase(Key.D0, ModifierKeys.None)]
    [TestCase(Key.D9, ModifierKeys.None)]
    [TestCase(Key.A, ModifierKeys.None)]
    [TestCase(Key.Z, ModifierKeys.None)]
    [TestCase(Key.A, ModifierKeys.Control)]
    [TestCase(Key.A, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows)]
    public void ShouldSerializeKeyboard(Key key, ModifierKeys modifierKeys)
    {
        // Given
        var instance = CreateInstance();
        var hotkey = new HotkeyGesture(key, modifierKeys);

        // When
        var result = instance.ConvertFrom(instance.ConvertToString(hotkey));

        // Then
        result.ShouldNotBeNull();
        result.ShouldBeOfType<HotkeyGesture>();
        result.ShouldBe(hotkey);
        result.ToString().ShouldBe(hotkey.ToString());
    }

    [Test]
    [TestCase("a", Key.A, ModifierKeys.None)]
    [TestCase("A", Key.A, ModifierKeys.None)]
    [TestCase("Enter", Key.Enter, ModifierKeys.None)]
    [TestCase("Ctrl+Enter", Key.Enter, ModifierKeys.Control)]
    [TestCase("-", Key.OemMinus, ModifierKeys.None)]
    [TestCase("`", Key.OemTilde, ModifierKeys.None)]
    [TestCase("=", Key.OemPlus, ModifierKeys.None)]
    [TestCase("/", Key.Divide, ModifierKeys.None)]
    [TestCase("*", Key.Multiply, ModifierKeys.None)]
    [TestCase("+", Key.OemPlus, ModifierKeys.None)]
    [TestCase("Ctrl+-", Key.OemMinus, ModifierKeys.Control)]
    [TestCase("D0", Key.D0, ModifierKeys.None)]
    [TestCase("D9", Key.D9, ModifierKeys.None)]
    [TestCase("9", Key.D9, ModifierKeys.None)]
    [TestCase("Num -", Key.Subtract, ModifierKeys.None)]
    [TestCase("Num +", Key.Add, ModifierKeys.None)]
    [TestCase("Num *", Key.Multiply, ModifierKeys.None)]
    [TestCase("Num 0", Key.NumPad0, ModifierKeys.None)]
    [TestCase("Num 1", Key.NumPad1, ModifierKeys.None)]
    [TestCase("Num 9", Key.NumPad9, ModifierKeys.None)]
    [TestCase("SHIFT+W", Key.W, ModifierKeys.Shift)]
    [TestCase("Ctrl+Shift+Num *", Key.Multiply, ModifierKeys.Control | ModifierKeys.Shift)]
    [TestCase("Shift+Ctrl+Num *", Key.Multiply, ModifierKeys.Control | ModifierKeys.Shift)]
    [TestCase("Ctrl+Shift+Num +", Key.Add, ModifierKeys.Control | ModifierKeys.Shift)]
    public void ShouldDeserializeKeyboard(string input, Key expected, ModifierKeys expectedModifiers)
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.ConvertFromString(input);

        //Then
        result.Key.ShouldBe(expected);
        result.ModifierKeys.ShouldBe(expectedModifiers);
    }
        
    [TestCase(MouseWheelAction.WheelDown, ModifierKeys.None, "WheelDown")]
    [TestCase(MouseWheelAction.WheelUp, ModifierKeys.None, "WheelUp")]
    [TestCase(MouseWheelAction.WheelDown, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows, "Ctrl+Alt+Shift+Windows+WheelDown")]
    [TestCase(MouseWheelAction.WheelUp, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows, "Ctrl+Alt+Shift+Windows+WheelDown")]
    public void ShouldSerializeMouseWheel(MouseWheelAction mouseWheel, ModifierKeys modifierKeys, string expected)
    {
        // Given
        var instance = CreateInstance();
        var hotkey = new HotkeyGesture(mouseWheel, modifierKeys);

        // When
        var result = instance.ConvertFrom(instance.ConvertToString(hotkey));

        // Then
        result.ShouldNotBeNull();
        result.ShouldBeOfType<HotkeyGesture>();
        result.ShouldBe(hotkey);
        result.ToString().ShouldBe(hotkey.ToString());
    }
        
    [TestCase("", MouseWheelAction.None, ModifierKeys.None)]
    [TestCase("WheelDown", MouseWheelAction.WheelDown, ModifierKeys.None)]
    [TestCase("WheelUp", MouseWheelAction.WheelUp, ModifierKeys.None)]
    [TestCase("Ctrl+WheelUp", MouseWheelAction.WheelUp, ModifierKeys.Control)]
    [TestCase("Ctrl+Shift+Num +", MouseWheelAction.None, ModifierKeys.Control | ModifierKeys.Shift)]
    [TestCase("Ctrl+Alt+Shift+Windows+WheelDown", MouseWheelAction.WheelDown, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows)]
    [TestCase("Ctrl+Shift+Alt+Windows+WheelDown", MouseWheelAction.WheelDown, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows)]
    [TestCase("Ctrl+Shift+Alt+Win+WheelDown", MouseWheelAction.WheelDown, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows)]
    public void ShouldDeserializeKeyboard(string input, MouseWheelAction expected, ModifierKeys expectedModifiers)
    {
        //Given
        var instance = CreateInstance();

        //When
        var result = instance.ConvertFromString(input);

        //Then
        result.MouseWheel.ShouldBe(expected);
        result.ModifierKeys.ShouldBe(expectedModifiers);
    }

    private HotkeyConverter CreateInstance()
    {
        return new HotkeyConverter();
    }
}