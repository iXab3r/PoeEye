using NUnit.Framework;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class StringExtensionsFixture : FixtureBase
{
    [Test]
    [TestCase("", 4, true, "")]
    [TestCase("a", 4, true, "a")]
    [TestCase("abcd", 4, true, "abcd")]
    [TestCase("abcde", 4, true, "abcd... (4+1 chars)")]
    [TestCase("abcde", 4, false, "abcd...")]
    [TestCase("abcdef", 5, true, "abcde... (5+1 chars)")]
    [TestCase("abcdef", 5, false, "abcde...")]
    public void ShouldTakeChars(string input, int maxChars, bool addSuffix, string expected)
    {
        //Given

        //When
        var result = input.TakeChars(maxChars, addSuffix: addSuffix);

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
        var result = input.TakeMidChars(maxChars, addSuffix: true);

        //Then
        result.ShouldBe(expected);
    }
}