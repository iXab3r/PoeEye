using System.Windows.Input;
using NUnit.Framework;
using PoeShared.UI.Hotkeys;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    public class HotkeyConverterTests
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
            var hotkey = new HotkeyGesture(key, ModifierKeys.None);

            // When
            var result = instance.ConvertFrom(hotkey.ToString());

            // Then
            result.ShouldNotBeNull();
            result.ShouldBeOfType<HotkeyGesture>();
            var resultHotkey = (HotkeyGesture) result;
            var expectedModifier = ModifierKeys.None;
            switch (key)
            {
                case Key.LWin:
                case Key.RWin:
                    expectedModifier = ModifierKeys.Windows;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    expectedModifier = ModifierKeys.Alt;
                    break;
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    expectedModifier = ModifierKeys.Control;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    expectedModifier = ModifierKeys.Shift;
                    break;
            }
            resultHotkey.ModifierKeys.ShouldBe(expectedModifier);
            if (expectedModifier == ModifierKeys.None && key != Key.None)
            {
                resultHotkey.Key.ShouldNotBe(Key.None);
            }
        }

        [TestCase(Key.None, ModifierKeys.None, "None")]
        [TestCase(Key.OemPlus, ModifierKeys.None, "+")]
        [TestCase(Key.OemMinus, ModifierKeys.None, "-")]
        [TestCase(Key.Divide, ModifierKeys.None, "/")]
        [TestCase(Key.Multiply, ModifierKeys.None, "*")]
        [TestCase(Key.A, ModifierKeys.None, "A")]
        [TestCase(Key.None, ModifierKeys.Control, "Ctrl")]
        [TestCase(Key.A, ModifierKeys.Control, "Ctrl+A")]
        [TestCase(Key.A, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows, "Ctrl+Alt+Shift+Windows+A")]
        public void ShouldSerializeKeyboard(Key key, ModifierKeys modifierKeys, string expected)
        {
            // Given
            var instance = CreateInstance();
            var hotkey = new HotkeyGesture(key, modifierKeys);

            // When
            var result = instance.ConvertFrom(hotkey.ToString());

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
        [TestCase("Num -", Key.Subtract, ModifierKeys.None)]
        [TestCase("Num +", Key.Add, ModifierKeys.None)]
        [TestCase("Num *", Key.Multiply, ModifierKeys.None)]
        [TestCase("Ctrl+Shift+Num *", Key.Multiply, ModifierKeys.Control | ModifierKeys.Shift)]
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
            var result = instance.ConvertFrom(hotkey.ToString());

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
}