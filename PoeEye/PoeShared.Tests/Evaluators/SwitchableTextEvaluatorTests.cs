using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Evaluators;
using Shouldly;

namespace PoeShared.Tests.Evaluators;

[TestFixture]
public class SwitchableTextEvaluatorTests : FixtureBase
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
    [TestCase( TextEvaluatorType.Lambda, null, null, false)]
    [TestCase( TextEvaluatorType.Regex, null, null, false)]
    [TestCase( TextEvaluatorType.Text, null, null, false)]
    [TestCase(TextEvaluatorType.Text, null, "", false)]
    [TestCase(TextEvaluatorType.Text, "", null, false)]
    [TestCase(TextEvaluatorType.Text, "", "", true)]
    [TestCase(TextEvaluatorType.Text, "a", "a", true)]
    [TestCase(TextEvaluatorType.Text, "a", "b", false)]
    [TestCase(TextEvaluatorType.Text, "a", "1", false)]
    [TestCase(TextEvaluatorType.Text, "1", "1", true)]
    [TestCase(TextEvaluatorType.Regex, "1", "\\d", true)]
    [TestCase(TextEvaluatorType.Lambda, "1", "x == \"1\"", true)]
    public void ShouldMatch(TextEvaluatorType type, string text, string expression, bool expected )
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Text = text;
        instance.Expression = expression;
        instance.EvaluatorType = type;

        //Then
        instance.IsMatch.ShouldBe(expected);
    }

    private SwitchableTextEvaluator CreateInstance()
    {
        return Container.Build<SwitchableTextEvaluator>().Create();
    }
}