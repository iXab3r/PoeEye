using System.Linq;
using NUnit.Framework;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class EnumerableExtensionsFixture
{
    [Test]
    [TestCase("", "")]
    [TestCase("1", "1")]
    [TestCase("11",  "11")]
    [TestCase("12", "12", "21")]
    [TestCase("123", "123", "213", "231", "132", "321", "312")]
    public void ShouldBuildPermutations(string sourceRaw, params string[] expected)
    {
        //Given
        var source = sourceRaw.Select(x => x.ToString()).ToArray();

        //When
        var result = source.ToPermutations().Select(x => x.JoinStrings(string.Empty)).ToArray();

        //Then
        result.OrderBy(x => x).ShouldBe(expected.OrderBy(x => x));
    }
        
    [Test]
    [TestCase("")]  
    [TestCase("1", "1")]
    [TestCase("12", "1", "2", "12", "21")]
    [TestCase("123", "1", "2", "3", "12", "13", "21", "31", "32", "23", "123", "132", "213", "231", "312", "321")]
    public void ShouldBuildVariations(string sourceRaw, params string[] expected)
    {
        //Given
        var source = sourceRaw.Select(x => x.ToString()).ToArray();

        //When
        var result = source.ToVariations().Select(x => x.JoinStrings(string.Empty)).ToArray();

        //Then
        result.OrderBy(x => x).ShouldBe(expected.OrderBy(x => x));
    }

    [Test]
    [TestCase("", "ab", new[] {"a", "b"})] // Empty separator
    [TestCase("-", "", new string[] {})] // Empty input array
    [TestCase("", "", new string[] {})] // Empty separator and input array
    [TestCase("-", "a", new[] {"a"})] // Single element, normal separator
    [TestCase("", "a", new[] {"a"})] // Single element, empty separator
    [TestCase("-", "a-", new[] {"a", ""})] // Empty string as second element
    [TestCase("-", "-b", new[] {"", "b"})] // Empty string as first element
    [TestCase("-", "-", new[] {"", ""})] // Only empty strings as elements
    [TestCase("-", "a-b", "a", "b")]
    [TestCase("-", "a-b", new[] {"a", "b"})]
    [TestCase("-", "a-b-c", new[] {"a", "b", "c"})]
    [TestCase("->", "1->2->3", new[] {"1", "2", "3"})]
    [TestCase(", ", "apple, banana, cherry", new[] {"apple", "banana", "cherry"})]
    [TestCase("", "123", new[] {"1", "2", "3"})] // No separator
    [TestCase(" ", "x y z", new[] {"x", "y", "z"})]
    [TestCase(" | ", "cat | dog | fish", new[] {"cat", "dog", "fish"})]
    [TestCase("-", "", new string[] {})] // Empty input array
    [TestCase("-", "single", new[] {"single"})] // Single element
    [TestCase("--", "first--second", new[] {"first", "second"})] // Multi-char separator
    public void ShouldIntersperse(string element, string expected, params string[] items)
    {
        //Given
        //When
        var result = items.Intersperse(element).ToArray().JoinStrings("");

        //Then
        result.ShouldBe(expected);
    }
}