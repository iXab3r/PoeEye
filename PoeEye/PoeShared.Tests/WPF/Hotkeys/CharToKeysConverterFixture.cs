using System.Collections.Generic;
using System.Windows.Forms;
using AutoFixture;
using NUnit.Framework;
using PoeShared.Services;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.WPF.Hotkeys
{
    [TestFixture]
    internal class CharToKeysConverterFixture : FixtureBase
    {
        private const string RussianLayoutId = "00000419";
        private const string EnglishLayoutId = "00000409";

        private IKeyboardLayoutManager keyboardLayoutManager;

        protected override void SetUp()
        {
            base.SetUp();

            keyboardLayoutManager = Container.Build<KeyboardLayoutManager>().Create(); // using real layout manager to test integration with WinApi
            Container.Register<IKeyboardLayoutManager>(() => keyboardLayoutManager);
        }

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
            var layout = keyboardLayoutManager.ResolveByLayoutName(EnglishLayoutId);
            keyboardLayoutManager.Activate(layout);

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
        [TestCase(' ', RussianLayoutId, Keys.Space)]
        [TestCase('ф', RussianLayoutId, Keys.A)]
        public void ShouldConvertWithLayout(char c, string keyboardLayoutName, Keys expected)
        {
            //Given
            var instance = CreateInstance();
            var layout = keyboardLayoutManager.ResolveByLayoutName(keyboardLayoutName);

            //When
            var result = instance.Convert((c,layout));

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
            return Container.Build<CharToKeysConverter>().Create();
        }
    }
}