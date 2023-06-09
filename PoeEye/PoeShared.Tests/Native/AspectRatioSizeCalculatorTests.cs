using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using PoeShared.Native;
using Shouldly;

namespace PoeShared.Tests.Native;

[TestFixture]
public class AspectRatioSizeCalculatorTests
{
    [Test]
    [TestCaseSource(nameof(ShouldCalculateCases))]
    public void ShouldCalculate(bool prioritizeHeight, double aspectRatio, Rectangle bounds, Rectangle expected)
    {
        //Given
        var instance = CreateInstance();

        var initialBounds = new Rectangle(0, 0, 10, 10);

        //When
        var result = instance.Calculate(aspectRatio, bounds, initialBounds, prioritizeHeight);

        //Then
        result.ShouldBe(expected, $"{new { prioritizeHeight, aspectRatio, bounds, expected }}");
    }

    private static IEnumerable<TestCaseData> ShouldCalculateCases()
    {
        yield return new TestCaseData(true, 1d, Rectangle.Empty, Rectangle.Empty);
        yield return new TestCaseData(true, 1d, new Rectangle(), new Rectangle());
            
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 10)) { TestName = "In-place scaling x1 - prioritize height" };
        yield return new TestCaseData(true, 2d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 5)) { TestName = "In-place scaling x2 - prioritize height" };
        yield return new TestCaseData(true, 1/2d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 20)) { TestName = "In-place scaling x1/2 - prioritize height" };
        yield return new TestCaseData(false, 2d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 20, 10)) { TestName = "In-place scaling x2 - prioritize width" };
        yield return new TestCaseData(false, 1/2d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 5, 10)) { TestName = "In-place scaling x1/2 - prioritize width" };
            
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 15, 15)) { TestName = "Basic scaling x1 - prioritize height" };
        yield return new TestCaseData(true, 2d, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 15, 7)) { TestName = "Basic scaling x2 - prioritize height" };
        yield return new TestCaseData(true, 1/2d, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 15, 30)) { TestName = "Basic scaling x1/2 - prioritize height" };
        yield return new TestCaseData(false, 2d, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 30, 15)) { TestName = "Basic scaling x2 - prioritize width" };
        yield return new TestCaseData(false, 1/2d, new Rectangle(0, 0, 15, 15), new Rectangle(0, 0, 7, 15)) { TestName = "Basic scaling x1/2 - prioritize width" };
            
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 10)) { TestName = "Initial assignment" };
        yield return new TestCaseData(true, 1d, new Rectangle(10, 0, 10, 10), new Rectangle(10, 0, 10, 10)) { TestName = "X move" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 10, 10, 10), new Rectangle(0, 10, 10, 10)) { TestName = "Y move" };
        yield return new TestCaseData(true, 1d, new Rectangle(10, 10, 10, 10), new Rectangle(10, 10, 10, 10)) { TestName = "X+Y move" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 20, 10), new Rectangle(0, 0, 20, 20)) { TestName = "Width increment" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 5, 10), new Rectangle(0, 0, 5, 5)) { TestName = "Width decrement" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 10, 20), new Rectangle(0, 0, 20, 20)) { TestName = "Height increment" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 10, 5), new Rectangle(0, 0, 5, 5)) { TestName = "Height decrement" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 20, 20), new Rectangle(0, 0, 20, 20)) { TestName = "Bottom-Right increment EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 25, 15), new Rectangle(0, 0, 25, 25)) { TestName = "Bottom-Right increment X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 15, 25), new Rectangle(0, 0, 15, 15)) { TestName = "Bottom-Right increment X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 5, 5), new Rectangle(0, 0, 5, 5)) { TestName = "Bottom-Right decrement EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 7, 5), new Rectangle(0, 0, 7, 7)) { TestName = "Bottom-Right decrement X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 0, 5, 7), new Rectangle(0, 0, 5, 5)) { TestName = "Bottom-Right decrement X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(-5, 0, 15, 15), new Rectangle(-5, 0, 15, 15)) { TestName = "Bottom-Left increment EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(-5, 0, 20, 15), new Rectangle(-5, 0, 20, 20)) { TestName = "Bottom-Left increment X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(-5, 0, 15, 20), new Rectangle(-5, 0, 15, 15)) { TestName = "Bottom-Left increment X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(5, 0, 5, 5), new Rectangle(5, 0, 5, 5)) { TestName = "Bottom-Left decrement EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(5, 0, 7, 5), new Rectangle(5, 0, 7, 7)) { TestName = "Bottom-Left decrement X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(5, 0, 5, 7), new Rectangle(5, 0, 5, 5)) { TestName = "Bottom-Left decrement X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(-5, -5, 15, 15), new Rectangle(-5, -5, 15, 15)) { TestName = "Top-Left increment EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(-10, -5, 20, 15), new Rectangle(-10, -10, 20, 20)) { TestName = "Top-Left increment X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(-5, -10, 15, 20), new Rectangle(-5, -5, 15, 15)) { TestName = "Top-Left increment X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(5, 5, 5, 5), new Rectangle(5, 5, 5, 5)) { TestName = "Top-Left decrement EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(3, 5, 7, 5), new Rectangle(3, 3, 7, 7)) { TestName = "Top-Left decrement X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(5, 3, 5, 7), new Rectangle(5, 5, 5, 5)) { TestName = "Top-Left decrement X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, -5, 15, 15), new Rectangle(0, -5, 15, 15)) { TestName = "Top-Right increment EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, -5, 20, 15), new Rectangle(0, -10, 20, 20)) { TestName = "Top-Right increment X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, -10, 15, 20), new Rectangle(0, -5, 15, 15)) { TestName = "Top-Right increment X < Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 5, 5, 5), new Rectangle(0, 5, 5, 5)) { TestName = "Top-Right decrement EQUAL" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 5, 7, 5), new Rectangle(0, 3, 7, 7)) { TestName = "Top-Right decrement X > Y" };
        yield return new TestCaseData(true, 1d, new Rectangle(0, 3, 5, 7), new Rectangle(0, 5, 5, 5)) {TestName = "Top-Right decrement X < Y"};
            
        yield return new TestCaseData(false, 1d, Rectangle.Empty, Rectangle.Empty);
        yield return new TestCaseData(false, 1d, new Rectangle(), new Rectangle());
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 10, 10), new Rectangle(0, 0, 10, 10)) { TestName = "Initial assignment" };
        yield return new TestCaseData(false, 1d, new Rectangle(10, 0, 10, 10), new Rectangle(10, 0, 10, 10)) { TestName = "X move" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 10, 10, 10), new Rectangle(0, 10, 10, 10)) { TestName = "Y move" };
        yield return new TestCaseData(false, 1d, new Rectangle(10, 10, 10, 10), new Rectangle(10, 10, 10, 10)) { TestName = "X+Y move" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 20, 10), new Rectangle(0, 0, 20, 20)) { TestName = "Width increment" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 5, 10), new Rectangle(0, 0, 5, 5)) { TestName = "Width decrement" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 10, 20), new Rectangle(0, 0, 20, 20)) { TestName = "Height increment" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 10, 5), new Rectangle(0, 0, 5, 5)) { TestName = "Height decrement" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 20, 20), new Rectangle(0, 0, 20, 20)) { TestName = "Bottom-Right increment EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 25, 15), new Rectangle(0, 0, 15, 15)) { TestName = "Bottom-Right increment X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 15, 25), new Rectangle(0, 0, 25, 25)) { TestName = "Bottom-Right increment X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 5, 5), new Rectangle(0, 0, 5, 5)) { TestName = "Bottom-Right decrement EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 7, 5), new Rectangle(0, 0, 5, 5)) { TestName = "Bottom-Right decrement X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 0, 5, 7), new Rectangle(0, 0, 7, 7)) { TestName = "Bottom-Right decrement X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(-5, 0, 15, 15), new Rectangle(-5, 0, 15, 15)) { TestName = "Bottom-Left increment EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(-5, 0, 20, 15), new Rectangle(0, 0, 15, 15)) { TestName = "Bottom-Left increment X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(-5, 0, 15, 20), new Rectangle(-10, 0, 20, 20)) { TestName = "Bottom-Left increment X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(5, 0, 5, 5), new Rectangle(5, 0, 5, 5)) { TestName = "Bottom-Left decrement EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(5, 0, 7, 5), new Rectangle(7, 0, 5, 5)) { TestName = "Bottom-Left decrement X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(5, 0, 5, 7), new Rectangle(3, 0, 7, 7)) { TestName = "Bottom-Left decrement X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(-5, -5, 15, 15), new Rectangle(-5, -5, 15, 15)) { TestName = "Top-Left increment EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(-10, -5, 20, 15), new Rectangle(-5, -5, 15, 15)) { TestName = "Top-Left increment X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(-5, -10, 15, 20), new Rectangle(-10, -10, 20, 20)) { TestName = "Top-Left increment X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(5, 5, 5, 5), new Rectangle(5, 5, 5, 5)) { TestName = "Top-Left decrement EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(3, 5, 7, 5), new Rectangle(5, 5, 5, 5)) { TestName = "Top-Left decrement X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(5, 3, 5, 7), new Rectangle(3, 3, 7, 7)) { TestName = "Top-Left decrement X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, -5, 15, 15), new Rectangle(0, -5, 15, 15)) { TestName = "Top-Right increment EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, -5, 20, 15), new Rectangle(0, -5, 15, 15)) { TestName = "Top-Right increment X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, -10, 15, 20), new Rectangle(0, -10, 20, 20)) { TestName = "Top-Right increment X < Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 5, 5, 5), new Rectangle(0, 5, 5, 5)) { TestName = "Top-Right decrement EQUAL" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 5, 7, 5), new Rectangle(0, 5, 5, 5)) { TestName = "Top-Right decrement X > Y" };
        yield return new TestCaseData(false, 1d, new Rectangle(0, 3, 5, 7), new Rectangle(0, 3, 7, 7)) {TestName = "Top-Right decrement X < Y"};
    }

    private AspectRatioSizeCalculator CreateInstance()
    {
        return new AspectRatioSizeCalculator();
    } 
}