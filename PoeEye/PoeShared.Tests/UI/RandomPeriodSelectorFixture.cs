﻿using System;
using Moq;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.UI
{
    [TestFixture]
    public class RandomPeriodSelectorFixture
    {
        private Mock<IRandomNumberGenerator> rng;

        [SetUp]
        public void SetUp()
        {
            rng = new Mock<IRandomNumberGenerator>();
        }

        [Test]
        [TestCase(0, -10, 10, 0, null)]
        [TestCase(10, -10, 10, 11, null)]
        [TestCase(-10, -10, 10, -11, null)]
        public void ShouldReturnLowerValueWhenNotRandomized(int expected, int minimum, int maximum, int? lower, int? upper)
        {
            //Given
            var instance = CreateInstance();
            var expectedValue = TimeSpan.FromSeconds(expected);
            instance.Minimum = TimeSpan.FromSeconds(minimum);
            instance.Maximum = TimeSpan.FromSeconds(maximum);
            if (lower != null)
            {
                instance.LowerValue = TimeSpan.FromSeconds(lower.Value);
            }

            if (upper != null)
            {
                instance.UpperValue = TimeSpan.FromSeconds(upper.Value);
            }

            //When
            var result = instance.GetValue();

            //Then
            result.ShouldBe(expectedValue);
        }
        
        public IRandomPeriodSelector CreateInstance()
        {
            return new RandomPeriodSelector(rng.Object);
        }
    }
}