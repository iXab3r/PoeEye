using NUnit.Framework;
using AutoFixture;
using System;
using System.Globalization;
using System.Linq;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Services
{
    [TestFixture]
    public class KeyboardLayoutManagerFixture : FixtureBase
    {
        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        public void ShouldReturnCurrent()
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.GetCurrent();

            //Then
            result.IsValid.ShouldBe(true);
        }

        [Test]
        public void ShouldHaveLayouts()
        {
            //Given

            //When
            var instance = CreateInstance();


            //Then
            instance.KnownLayouts.Count.ShouldBeGreaterThan(0);
        }

        [Test]
        public void ShouldResolveByLayoutId()
        {
            //Given
            var instance = CreateInstance();
            var layouts = instance.KnownLayouts.ToArray();

            //When
            //Then
            foreach (var keyboardLayout in layouts)
            {
                var resolved = instance.ResolveByLayoutName(keyboardLayout.LayoutName);
                resolved.ShouldBe(keyboardLayout);
                resolved.IsValid.ShouldBe(true);
            }
        }

        [Test]
        public void ShouldActivateLayouts()
        {
            //Given
            var instance = CreateInstance();
            var layouts = instance.KnownLayouts.ToArray();

            //When
            //Then
            foreach (var keyboardLayout in layouts)
            {
                instance.Activate(keyboardLayout);
                var current = instance.GetCurrent();
                current.ShouldBe(keyboardLayout);
            }
        }

        [Test]
        public void ShouldResolveCultureByLayout()
        {
            //Given
            var instance = CreateInstance();
            var layouts = instance.KnownLayouts;

            //When
            //Then
            foreach (var keyboardLayout in layouts)
            {
                var resolved = instance.ResolveByCulture(keyboardLayout.Culture);
                resolved.ShouldBe(keyboardLayout);
            }
        }

        private KeyboardLayoutManager CreateInstance()
        {
            return Container.Build<KeyboardLayoutManager>().Create();
        }
    }
}