using System.Collections.Generic;
using System.Drawing;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class GeometryExtensionsFixture : FixtureBase
{
    [Test]
    [TestCaseSource(nameof(ShouldFitInsideCases))]
    public void ShouldFitInside(Rectangle rectangle, Rectangle region, Rectangle expected)
    {
        //Given
        //When
        var result = rectangle.PickRegion(region);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    [TestCaseSource(nameof(InflateScaleCases))]
    public void ShouldInflateScaleCorrectly(Rectangle sourceSize, float widthMultiplier, float heightMultiplier, Rectangle expected)
    {
        // Given
        // When
        var result = sourceSize.InflateScale(widthMultiplier, heightMultiplier);

        // Then
        result.ShouldBe(expected);
    }

    public static IEnumerable<TestCaseData> InflateScaleCases()
    {
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 2f, 2f, new Rectangle(-50, -50, 200, 200)).SetName("Double size scaling");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 0.5f, 0.5f, new Rectangle(25, 25, 50, 50)).SetName("Half size scaling");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 0f, 0f, new Rectangle(50, 50, 0, 0)).SetName("Scaling to zero");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), -1f, -1f, new Rectangle(100, 100, -100, -100)).SetName("Negative scaling — fully shrunk");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), -0.5f, -0.5f, new Rectangle(75, 75, -50, -50)).SetName("Negative scaling — partially shrunk");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 1f, 1f, new Rectangle(0, 0, 100, 100)).SetName("No scaling (identity)");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 1.5f, 0.5f, new Rectangle(-25, 25, 150, 50)).SetName("Asymmetric scaling - increase width, reduce height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), 0.5f, 2f, new Rectangle(25, -50, 50, 200)).SetName("Asymmetric scaling - reduce width, increase height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 0), 2f, 2f, new Rectangle(-50, 0, 200, 0)).SetName("Scaling zero height");
        yield return new TestCaseData(new Rectangle(0, 0, 0, 100), 2f, 2f, new Rectangle(0, -50, 0, 200)).SetName("Scaling zero width");
    }

    public static IEnumerable<TestCaseData> ShouldFitInsideCases()
    {
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 50, 50), new Rectangle(0, 0, 50, 50)).SetName("Fit a larger rectangle inside a smaller one");
        yield return new TestCaseData(new Rectangle(0, 0, 50, 50), new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 50, 50)).SetName("Fit a smaller rectangle inside a larger one");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, -10, -10), new Rectangle(0, 0, 90, 90)).SetName("Shrink a rectangle by negative region values");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(10, 10, 80, 80), new Rectangle(10, 10, 80, 80)).SetName("Offset and fit inside");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(50, 50, 0, 0), new Rectangle(50, 50, 50, 50)).SetName("Offset without resizing");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 100, 100)).SetName("Region with zero dimensions should return original");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(10, 10, -10, -10), new Rectangle(10, 10, 80, 80)).SetName("Offset and shrink by negative region values");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(-10, -10, 120, 120), new Rectangle(0, 0, 100, 100)).SetName("Region out of bounds with positive dimensions");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(10, 10, -110, -110), new Rectangle(10, 10, 0, 0)).SetName("Region out of bounds with negative dimensions");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(-10, -10, 110, 110), new Rectangle(0, 0, 100, 100)).SetName("Region with negative offset and positive dimensions");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(10, 10, -20, -20), new Rectangle(10, 10, 70, 70)).SetName("Region with positive offset and negative dimensions");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 200, 200), new Rectangle(0, 0, 100, 100)).SetName("Region larger than bounds in both dimensions");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 50, 200), new Rectangle(0, 0, 50, 100)).SetName("Region larger than bounds in one dimension");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 200, 50), new Rectangle(0, 0, 100, 50)).SetName("Region larger than bounds in other dimension");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(50, 50, -20, -20), new Rectangle(50, 50, 30, 30)).SetName("Offset with negative width and height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(50, 50, 20, 20), new Rectangle(50, 50, 20, 20)).SetName("Offset with positive width and height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(-50, -50, 50, 50), new Rectangle(0, 0, 0, 0)).SetName("Negative offset with positive width and height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(-50, -50, -20, -20), new Rectangle(0, 0, 0, 0)).SetName("Negative offset with negative width and height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 0, 50), new Rectangle(0, 0, 100, 50)).SetName("Zero width with positive height");
        yield return new TestCaseData(new Rectangle(0, 0, 100, 100), new Rectangle(0, 0, 50, 0), new Rectangle(0, 0, 50, 100)).SetName("Positive width with zero height");
    }
}