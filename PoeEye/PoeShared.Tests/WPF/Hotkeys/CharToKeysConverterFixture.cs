using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using PoeShared.UI.Hotkeys;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    internal class CharToKeysConverterFixture
    {
        private const string RussianLayoutId = "00000419";
        private const string EnglishLayoutId = "00000409";
        
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
        [TestCase('a', EnglishLayoutId, Keys.A)]
        [TestCase('z', EnglishLayoutId, Keys.Z)]
        [TestCase('Z', EnglishLayoutId, Keys.Z | Keys.Shift)]
        [TestCase('A', EnglishLayoutId, Keys.A | Keys.Shift)]
        [TestCase(' ', EnglishLayoutId, Keys.Space)]
        public void ShouldConvertWithLayout(char c, string keyboardLayoutId, Keys expected)
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.Convert((c,keyboardLayoutId));

            //Then
            result.ShouldBe(expected);
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
        
        public CharToKeysConverter CreateInstance()
        {
            return new CharToKeysConverter();
        }
    }
}