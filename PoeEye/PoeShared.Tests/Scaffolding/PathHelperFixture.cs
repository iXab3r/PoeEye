using NUnit.Framework;
using AutoFixture;
using System;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class PathHelperFixture : FixtureBase
{
    [Test]
    [TestCase(null, null, true)]
    [TestCase("", "", true)]
    [TestCase("a", "a", true)]
    [TestCase("a", "b", false)]
    [TestCase("a\\b\\c", "a\\b\\c", true)]
    [TestCase("a\\b", "a\\b\\c", true)]
    [TestCase("a", "a\\b\\c", true)]
    [TestCase("b\\c", "a\\b\\c", false)]
    [TestCase("b", "a\\b\\c", false)]
    [TestCase("a", "abc\\b\\c", false)]
    [TestCase("alpha\\beta", "alpha\\beta\\Aura #3", true)]
    public void ShouldCheckSubfolder(string folderPath, string fullPath, bool expected)
    {
        //Given
        //When
        var result = PathUtils.IsSubpath(folderPath, fullPath);

        //Then
        result.ShouldBe(expected);
    }
}