using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Evaluators;
using Shouldly;

namespace PoeShared.Tests.Evaluators;

[TestFixture]
public class TextRegexEvaluatorTests : FixtureBase
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
    [TestCase("", "", true)]
    [TestCase("a", "a", true)]
    [TestCase("a", "b", false)]
    [TestCase("a", "\\d", false)]
    [TestCase("1", "\\d", true)]
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
    
    [TestCase(null, null, null)]
    [TestCase(null, "", null)]
    [TestCase("", null, null)]
    [TestCase("a", "b", null)]
    [TestCase("", "", "")]
    [TestCase("a", "a", "a")]
    [TestCase("a", "(a)", "a")]
    [TestCase("a", "(?:a)", "a")]
    [TestCase("a", "(?'test'a)", "a")]
    [TestCase("a b", "a", "a")]
    [TestCase("a b", "a b", "a b")]
    [TestCase("a b", "a (b)", "b")]
    [TestCase("a b", "(?:a) (b)", "b")]
    [TestCase("a b", "(?:a) (?'test'b)", "b")]
    [TestCase("a b", "a (?'test'b)", "b")]
    [TestCase("a b", "a (?:b)", "a b")]
    [TestCase("a b c", "a (b) (c)", "b")]
    [TestCase("a b c", "(?:a) (b) (c)", "b")]
    public void ShouldFillMatch(string text, string expression, string expected )
    {
        //Given
        var instance = CreateInstance();

        //When
        instance.Text = text;
        instance.Expression = expression;

        //Then
        instance.Match.ShouldBe(expected);
    }

    private TextRegexEvaluator CreateInstance()
    {
        return Container.Build<TextRegexEvaluator>().Create();
    }
}