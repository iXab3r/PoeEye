using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class PathUtilsFixture : FixtureBase
{
    [Test]
    [TestCase(null, null)]
    [TestCase(null, "")]
    [TestCase(null, "a", "b")]
    [TestCase("a", "a")]
    [TestCase("a", "a\\b", "a\\c")]
    [TestCase("a", "a\\b\\c", "a\\c\\d")]
    [TestCase("a\\b", "a\\b")]
    [TestCase("a\\b", "a\\b", "a\\b\\c")]
    public void ShouldGetCommonRootDirectory(string expected, params string[] paths)
    {
        //Given
        //When
        var resultSupplier = () => PathUtils.GetCommonRootDirectory(paths);

        //Then
        if (expected == null)
        {
            resultSupplier.ShouldThrow<Exception>();
        }
        else
        {
            resultSupplier().ShouldBe(expected);
        }
    }
    
    [Test]
    [TestCase(null, null, true)]
    [TestCase("", "", true)]
    [TestCase("", "a", false)]
    [TestCase("a", "", false)]
    [TestCase("a", "a", true)]
    [TestCase("a", "b", false)]
    [TestCase("a\\b\\c", "a\\b\\c", true)]
    [TestCase("a\\b", "a\\b\\c", true)]
    [TestCase("a", "a\\b\\c", true)]
    [TestCase("b\\c", "a\\b\\c", false)]
    [TestCase("b", "a\\b\\c", false)]
    [TestCase("a", "abc\\b\\c", false)]
    [TestCase("alpha\\beta", "alpha\\beta\\Aura #3", true)]
    public void ShouldCheck(string folderPath, string fullPath, bool expected)
    {
        //Given
        //When
        var result = PathUtils.IsDirOrSubDir(folderPath, fullPath);

        //Then
        result.ShouldBe(expected);
    }
    
    [Test]
    [TestCase(null, null, false)]
    [TestCase("", "", false)]
    [TestCase("", "a", false)]
    [TestCase("a", "", false)]
    [TestCase("a", "a", false)]
    [TestCase("a", "b", false)]
    [TestCase("a\\b\\c", "a\\b\\c", false)]
    [TestCase("b\\c", "a\\b\\c", true)]
    [TestCase("b", "a\\b\\c", true)]
    [TestCase("a", "abc\\bcd\\c", false)]
    [TestCase("b", "abc\\bcd\\c", false)]
    [TestCase("beta", "alpha\\beta\\Aura #3", true)]
    public void ShouldCheckSubfolder(string folderPath, string fullPath, bool expected)
    {
        //Given
        //When
        var result = PathUtils.IsSubDir(folderPath, fullPath);

        //Then
        result.ShouldBe(expected);
    }
    
    [Test]
    [TestCase(null, null, false)]
    [TestCase("", "", false)]
    [TestCase("", "a", false)]
    [TestCase("a", "", false)]
    [TestCase("a", "a", false)]
    [TestCase("a", "b", false)]
    [TestCase("a\\b\\c", "a\\b\\c", false)]
    [TestCase("a", "a\\b\\c", true)]
    [TestCase("a\\b", "a\\b\\c", true)]
    [TestCase("b", "a\\b\\c", false)]
    [TestCase("a", "abc\\bcd\\c", false)]
    [TestCase("alpha", "alpha\\beta\\Aura #3", true)]
    public void ShouldCheckParent(string folderPath, string fullPath, bool expected)
    {
        //Given
        //When
        var result = PathUtils.IsParentDir(folderPath, fullPath);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    [TestCase("a", "a")]
    [TestCase("a", "a", "b")]
    [TestCase("f\\a", "f\\a")]
    [TestCase("f\\a", "f\\a", "a", "f\\b")]
    [TestCase("f\\a", "f\\a (2)", "f\\a", "f\\b")]
    [TestCase("a", "a (2)", "a")]
    [TestCase("a", "a (2)", "b", "a")]
    public void ShouldGenerateValidName(string candidate, string expected, params string[] existing)
    {
        //Given
        var existingPaths = new HashSet<string>(existing);

        //When
        var result = PathUtils.GenerateValidName(candidate, x => !existingPaths.Contains(x));

        //Then
        result.ShouldBe(expected);
    }
}