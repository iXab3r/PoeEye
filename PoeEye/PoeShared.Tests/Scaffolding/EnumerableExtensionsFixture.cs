using System;
using System.Linq;
using NUnit.Framework;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
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
}