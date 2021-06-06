using System.Windows.Forms;
using System.Windows.Input;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    public class KeysToKeyConverterFixture
    {
        [Test]
        [TestCase(Keys.None, Key.None)]
        [TestCase(Keys.A, Key.A)]
        [TestCase(Keys.Z, Key.Z)]
        [TestCase(Keys.A | Keys.Shift, Key.A)]
        [TestCase(Keys.A | Keys.Control, Key.A)]
        [TestCase(Keys.A | Keys.Alt, Key.A)]
        public void ShouldConvert(Keys input, Key expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(input);

            //Then
            result.ShouldBe(expected);
        }

        private KeysToKeyConverter CreateInstance()
        {
            return new();
        }
    }
}