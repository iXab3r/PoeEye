using NUnit.Framework;
using AutoFixture;
using System;
using System.ComponentModel;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.Scaffolding
{
    public class FallbackFixture : FixtureBase
    {
        protected override void SetUp()
        {
        }

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance().ShouldNotBeNull();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        [TestCase(null, null, null)]
        [TestCase("a", null, "a")]
        [TestCase("a", "b", "b")]
        [TestCase(null, "b", "b")]
        public void ShouldReturnValue(string defaultValue, string value, string expected)
        {
            //Given
            var instance = CreateInstance();
            instance.SetDefaultValue(defaultValue);
            instance.SetValue(value);

            //When
            var result = instance.Value;

            //Then
            result.ShouldBe(expected);
        }

        [Test]
        [TestCase(null, false)]
        [TestCase("b", true)]
        public void ShouldReturnHasValue(string value, bool expected)
        {
            //Given
            var instance = CreateInstance();
            instance.SetValue(value);

            //When
            var result = instance.HasActualValue;

            //Then
            result.ShouldBe(expected);
        }

        private Fallback<string> CreateInstance()
        {
            return Container.Create<Fallback<string>>();
        }
    }
}