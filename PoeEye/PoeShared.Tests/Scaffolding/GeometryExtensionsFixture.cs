using System.Collections.Generic;
using System.Drawing;
using PoeShared.Tests.Helpers;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
internal class GeometryExtensionsFixtureTests : FixtureBase
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