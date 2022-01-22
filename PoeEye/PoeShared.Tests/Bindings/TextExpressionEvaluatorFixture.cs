using NUnit.Framework;
using PoeShared.Evaluators;
using Shouldly;

namespace PoeShared.Tests.Bindings;

[TestFixture]
public class TextExpressionEvaluatorFixture : FixtureBase
{
    [Test]
    [TestCase(null, null, false)]
    [TestCase("x == \"test\"", "test", true)]
    [TestCase("Int32.Parse(x) == 10", "10", true)]
    public void ShouldEvaluate(string expression, string text, bool expected)
    {
        //Given
        var evaluator = new TextExpressionEvaluator();

        //When
        evaluator.Expression = expression;
        evaluator.Text = text;

        //Then
        evaluator.IsMatch.ShouldBe(expected);
    }
}