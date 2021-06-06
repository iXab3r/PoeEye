using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    internal class KeysToHotkeyGestureConverterFixture
    {
        [Test]
        [TestCaseSource(nameof(ShouldConvertCases))]
        public void ShouldConvert(Keys input, HotkeyGesture expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(input);

            //Then
            result.ShouldBe(expected);
        }

        public static IEnumerable<TestCaseData> ShouldConvertCases()
        {
            yield return new TestCaseData(Keys.None, HotkeyGesture.Empty);
            yield return new TestCaseData(Keys.A, new HotkeyGesture(Key.A));
            yield return new TestCaseData(Keys.Z, new HotkeyGesture(Key.Z));
            yield return new TestCaseData(Keys.A | Keys.Shift, new HotkeyGesture(Key.A, ModifierKeys.Shift));
            yield return new TestCaseData(Keys.C | Keys.Control, new HotkeyGesture(Key.C, ModifierKeys.Control));
            yield return new TestCaseData(Keys.Insert | Keys.Shift, new HotkeyGesture(Key.Insert, ModifierKeys.Shift));
        }

        private KeysToHotkeyGestureConverter CreateInstance()
        {
            return new();
        }
    }
}