using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Evaluators;
using Shouldly;

namespace PoeShared.Tests.Evaluators;

[TestFixture]
public class TextExpressionEvaluatorTests : FixtureBase
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
    [TestCase(null, null, false)]
    [TestCase(null, "", false)]
    [TestCase("", null, false)]
    [TestCase("", "", false)]
    [TestCase("a", "true", true)]
    [TestCase("a", "false", false)]
    [TestCase("a", "x == \"a\"", true)]
    [TestCase("a", "x == \"\b\"", false)]
    [TestCase("a", "b", false)]
    [TestCase("a", "1", false)]
    public void ShouldMatch(string text, string expression, bool expected )
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Text = text;
        instance.Expression = expression;

        //Then
        instance.IsMatch.ShouldBe(expected);
    }

    private TextExpressionEvaluator CreateInstance()
    {
        return Container.Build<TextExpressionEvaluator>().Create();
    }
}