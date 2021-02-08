using System;
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
        [TestCase(0, 0, 0)]
        [TestCase(1, 0 , 1)]
        [TestCase(10, 0 , 10)]
        [TestCase(11, 0 , 10)]
        [TestCase(-1, -1 , -1)]
        [TestCase(-10, -10 , -10)]
        [TestCase(-11, -10 , -10)]
        public void ShouldRespectMinMaxForUpper(int upper, int expectedLower, int expectedUpper)
        {
            //Given
            var instance = CreateInstance();
            instance.Minimum = TimeSpan.FromSeconds(-10);
            instance.Maximum = TimeSpan.FromSeconds(10);
            instance.RandomizeValue = true;

            //When
            instance.UpperValue = TimeSpan.FromSeconds(upper);

            //Then
            instance.UpperValue.ShouldBe(TimeSpan.FromSeconds(expectedUpper));
            instance.LowerValue.ShouldBe(TimeSpan.FromSeconds(expectedLower));
        }
        
        [Test]
        [TestCase(0, 0, false)]
        [TestCase(-1, -1, true)]
        [TestCase(1, 1, true)]
        public void ShouldNotAllowUpperToBeLessThanLower(int upper, int expectedUpper, bool expectedRandomize)
        {
            //Given
            var instance = CreateInstance();
            instance.RandomizeValue = false;

            //When
            instance.UpperValue = TimeSpan.FromSeconds(upper);

            //Then
            instance.UpperValue.ShouldBe(TimeSpan.FromSeconds(expectedUpper));
            instance.RandomizeValue.ShouldBe(expectedRandomize);
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(-1, -1)]
        [TestCase(-10, -10)]
        [TestCase(-11, -10)]
        [TestCase(10, 10)]
        [TestCase(11,  10)]
        public void ShouldRespectMinMaxForLower(int lower, int expectedLower)
        {
            //Given
            var instance = CreateInstance();
            instance.Minimum = TimeSpan.FromSeconds(-10);
            instance.Maximum = TimeSpan.FromSeconds(10);
            instance.RandomizeValue = false;

            //When
            instance.LowerValue = TimeSpan.FromSeconds(lower);

            //Then
            instance.LowerValue.ShouldBe(TimeSpan.FromSeconds(expectedLower));
            instance.UpperValue.ShouldBe(TimeSpan.FromSeconds(expectedLower));
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