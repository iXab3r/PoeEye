using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class StringExtensionsFixture : FixtureBase
{
    [Test]
    [TestCase("", 4, "")]
    [TestCase("a", 4, "a")]
    [TestCase("abcd", 4, "abcd")]
    [TestCase("abcde", 4, "abcd... (4+1 chars)")]
    [TestCase("abcdef", 5, "abcde... (5+1 chars)")]
    public void ShouldTakeChars(string input, int maxChars, string expected)
    {
        //Given

        //When
        var result = input.TakeChars(maxChars);

        //Then
        result.ShouldBe(expected);
    }
    
    [Test]
    [TestCase(null, 4, null)]
    [TestCase("", 4, "")]
    [TestCase("a", 4, "a")]
    [TestCase("abcd", 4, "abcd")]
    [TestCase("abcde", 4, "ab...de (4+1 chars)")]
    [TestCase("abcdef", 4, "ab...ef (4+2 chars)")]
    [TestCase("abcdefg", 4, "ab...fg (4+3 chars)")]
    [TestCase("abcdef", 5, "abc...ef (5+1 chars)")]
    [TestCase("abcdefg", 5, "abc...fg (5+2 chars)")]
    public void ShouldTakeMidChars(string input, int maxChars, string expected)
    {
        //Given

        //When
        var result = input.TakeMidChars(maxChars);

        //Then
        result.ShouldBe(expected);
    }
}