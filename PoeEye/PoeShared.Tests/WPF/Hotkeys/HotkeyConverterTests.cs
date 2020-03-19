using System.Windows.Input;
using NUnit.Framework;
using PoeShared.UI.Hotkeys;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    public class HotkeyConverterTests
    {
        [TestCase(Key.None, ModifierKeys.None, "None")]
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
        [TestCase("A", Key.A, ModifierKeys.None)]
        [TestCase("Enter", Key.Enter, ModifierKeys.None)]
        [TestCase("Ctrl+Enter", Key.Enter, ModifierKeys.Control)]
        [TestCase("-", Key.OemMinus, ModifierKeys.None)]
        [TestCase("`", Key.OemTilde, ModifierKeys.None)]
        [TestCase("=", Key.OemPlus, ModifierKeys.None)]
        [TestCase("/", Key.Divide, ModifierKeys.None)]
        [TestCase("*", Key.Multiply, ModifierKeys.None)]
        [TestCase("Numpad0", Key.NumPad0, ModifierKeys.None)]
        [TestCase("+", Key.OemPlus, ModifierKeys.None)]
        [TestCase("Ctrl+-", Key.OemMinus, ModifierKeys.Control)]
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

        private HotkeyConverter CreateInstance()
        {
            return new HotkeyConverter();
        }
    }
}