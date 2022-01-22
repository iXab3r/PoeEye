using System;
using Moq;
using NUnit.Framework;
using PoeShared.UI;
using Shouldly;

namespace PoeShared.Tests.UI;

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
    public void ShouldKeepUpperIfNotRandomized()
    {
        //Given
        var instance = CreateInstance();
        instance.RandomizeValue = true;
        instance.LowerValue = TimeSpan.FromSeconds(5);
        instance.UpperValue = TimeSpan.FromSeconds(7);

        //When
        instance.RandomizeValue = false;

        //Then
        instance.UpperValue.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Test]
    public void ShouldNotResetUpperValueIfRandomizeEnabled()
    {
        //Given
        var instance = CreateInstance();
        instance.LowerValue = TimeSpan.FromSeconds(5);
        instance.UpperValue = TimeSpan.FromSeconds(7);

        //When
        instance.RandomizeValue = true;

        //Then
        instance.LowerValue = TimeSpan.FromSeconds(5);
        instance.UpperValue = TimeSpan.FromSeconds(7);
    }

    [Test]
    [TestCase(10, 10, 10)]
    [TestCase(10, 11, 11)]
    [TestCase(11, 10, 10)]
    public void ShouldSetUpperToLowerWhenRandomizationIsDisabled(int lower, int upper, int upperExpected)
    {
        //Given
        var instance = CreateInstance();
        instance.RandomizeValue = false;
        instance.UpperValue = TimeSpan.FromSeconds(upper);

        //When
        instance.LowerValue = TimeSpan.FromSeconds(lower);

        //Then
        instance.UpperValue.ShouldBe(TimeSpan.FromSeconds(upperExpected));
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