using System.Collections.Generic;
using System.Globalization;
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
        private const string RussianCultureISO = "ru";
        private const string EnglishCultureISO = "en";

        private IKeyboardLayoutManager keyboardLayoutManager;

        protected override void SetUp()
        {
            base.SetUp();

            keyboardLayoutManager = Container.Build<KeyboardLayoutManager>().Create(); // using real layout manager to test integration with WinApi
            Container.Register<IKeyboardLayoutManager>(() => keyboardLayoutManager);
        }

        [Test]
        [TestCase('a', Keys.A)]
        [TestCase('0', Keys.D0)]
        [TestCase('9', Keys.D9)]
        [TestCase('z', Keys.Z)]
        [TestCase('Z', Keys.Z | Keys.Shift)]
        [TestCase('A', Keys.A | Keys.Shift)]
        [TestCase(' ', Keys.Space)]
        public void ShouldConvert(char c, Keys key)
        {
            //Given
            var instance = CreateInstance();
            var layout = keyboardLayoutManager.ResolveByCulture(new CultureInfo(EnglishCultureISO));
            keyboardLayoutManager.Activate(layout);

            //When
            var result = instance.Convert(c);

            //Then
            result.ShouldBe(key);
        }
        
        [Test]
        [TestCase('a', EnglishCultureISO, Keys.A)]
        [TestCase('z', EnglishCultureISO, Keys.Z)]
        [TestCase('Z', EnglishCultureISO, Keys.Z | Keys.Shift)]
        [TestCase('A', EnglishCultureISO, Keys.A | Keys.Shift)]
        [TestCase(' ', EnglishCultureISO, Keys.Space)]
        [TestCase(' ', RussianCultureISO, Keys.Space)]
        [TestCase('ф', RussianCultureISO, Keys.A)]
        public void ShouldConvertWithLayout(char c, string cultureIso, Keys expected)
        {
            //Given
            var instance = CreateInstance();
            var layout = keyboardLayoutManager.ResolveByCulture(new CultureInfo(cultureIso));

            //When
            var result = instance.Convert((c,layout));

            //Then
            result.ShouldBe(expected);
        }
        
        public CharToKeysConverter CreateInstance()
        {
            return Container.Build<CharToKeysConverter>().Create();
        }
    }
}