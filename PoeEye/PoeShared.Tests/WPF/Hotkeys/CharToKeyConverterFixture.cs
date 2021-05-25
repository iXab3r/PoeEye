using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using PoeShared.UI.Hotkeys;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    internal class CharToKeyConverterFixture
    {
        [Test]
        [TestCase('a', Keys.A)]
        [TestCase('z', Keys.Z)]
        [TestCase('Z', Keys.Z | Keys.Shift)]
        [TestCase('A', Keys.A | Keys.Shift)]
        [TestCase(' ', Keys.Space)]
        public void ShouldConvert(char c, Keys key)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(c);

            //Then
            result.ShouldBe(key);
        }

        [Test]
        [TestCaseSource(nameof(ShouldConvertCases))]
        public void ShouldConvertAll(char c)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert(c);

            //Then
            result.ShouldNotBe(Keys.None);
        }

        public static IEnumerable<TestCaseData> ShouldConvertCases()
        {
            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                if (char.IsDigit(c) || char.IsNumber(c))
                {
                    yield return new TestCaseData(c);
                }
            }
        }
        
        public CharToKeyConverter CreateInstance()
        {
            return new CharToKeyConverter();
        }
    }
}