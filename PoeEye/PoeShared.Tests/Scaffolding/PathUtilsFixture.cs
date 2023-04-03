using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.IO;
using Meziantou.Framework;
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
    public void ShouldGetLongestCommonPath(string expected, params string[] paths)
    {
        //Given
        //When
        var resultSupplier = () => PathUtils.GetLongestCommonPath(paths);

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
    [TestCase(null, null)]
    [TestCase(null, "")]
    [TestCase(null, "a", "b")]
    [TestCase("a", "a")]
    [TestCase("a", "a\\b", "a\\c")]
    [TestCase("a", "a\\b\\c", "a\\c\\d")]
    [TestCase("a", "a\\b")]
    [TestCase("a", "a\\b", "a\\b\\c")]
    public void ShouldGetRootPath(string expected, params string[] paths)
    {
        //Given
        //When
        var resultSupplier = () => PathUtils.GetRootDirectory(paths);

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
        if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(fullPath))
        {
            new DirectoryInfo(folderPath).IsDirOrSubDir(new DirectoryInfo(fullPath)).ShouldBe(expected);
        }
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
    [TestCase("a", "a\\B\\c", true)]
    [TestCase("A", "a\\B\\c", true)]
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
        if (!string.IsNullOrEmpty(folderPath) && !string.IsNullOrEmpty(fullPath))
        {
            new DirectoryInfo(folderPath).IsParentOf(new DirectoryInfo(fullPath)).ShouldBe(expected);
        }
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

    [Test]
    [TestCase("a", "a")]
    [TestCase("a\\b", "a\\b")]
    [TestCase("a\\b\\c", "a\\b\\c")]
    [TestCase("a\\..", ".")]
    [TestCase("a\\b\\c\\..\\..", "a")]
    [TestCase("a\\b\\c\\..", "a\\b")]
    [TestCase("a\\.", "a")]
    [TestCase(".\\b", "b")]
    public void ShouldResolveRelativePath(string path, string expected)
    {
        //Given
        var rootPath = FullPath.FromPath(".");

        //When
        var absolutePath = FullPath.FromPath(path);

        var resultPath = absolutePath.MakePathRelativeTo(rootPath);


        //Then
        resultPath.ToString().ShouldBe(expected);
    }

    [Test]
    [TestCase("a", "a")]
    [TestCase("a\\b", "a\\b")]
    [TestCase("a\\b\\c", "a\\b\\c")]
    [TestCase("a\\b\\c\\..\\..", "a")]
    [TestCase("a\\b\\c\\..", "a\\b")]
    [TestCase("a\\.", "a")]
    [TestCase("a\\..", ".")]
    [TestCase(".", "z\\y")]
    [TestCase("..", "z")]
    [TestCase(".\\b", "z\\y\\b")]
    [TestCase("..\\b", "z\\b")]
    [TestCase("..\\..\\b", "b")]
    [TestCase(".\\..\\..\\b", "b")]
    public void ShouldExpandPath(string path, string expected)
    {
        //Given
        var rootPath = "z\\y";

        //When
        var resultPath = PathUtils.ExpandPath(rootPath, path);


        //Then
        resultPath.ShouldBe(expected);
    }

    [Test]
    public void ShouldSupportEmptyRootWhenExpandingPath()
    {
        //Given
        //When
        var resultPath = PathUtils.ExpandPath(string.Empty, "a\\b\\c\\..");

        //Then
        resultPath.ShouldBe("a\\b");
    }

    [Test]
    [TestCase(".\\..\\..\\..\\b")]
    public void ShouldThrowWhenPathIsNotValid(string path)
    {
        //Given
        var rootPath = "z\\y";

        //When
        var action = () => PathUtils.ExpandPath(rootPath, path);


        //Then
        action.ShouldThrow<FormatException>();
    }

    [Test]
    [TestCase("a", 0)]
    [TestCase("a\\b", 1)]
    [TestCase("a\\b\\c", 2)]
    [TestCase("a\\b\\c\\..\\..", 4)]
    [TestCase("a\\b\\c\\..", 3)]
    [TestCase("a\\.", 1)]
    [TestCase("a\\..", 1)]
    [TestCase(".", 0)]
    [TestCase("..", 0)]
    [TestCase("a/b", 1)]
    [TestCase("a/b/c", 2)]
    [TestCase("a/b/c/../..", 4)]
    [TestCase("a/b/c/..", 3)]
    [TestCase("a/.", 1)]
    [TestCase("a/..", 1)]
    public void ShouldGetDepth(string path, int expectedDepth)
    {
        //Given
        //When
        var depth = PathUtils.GetDepth(path);

        //Then
        depth.ShouldBe(expectedDepth);
    }

    [Test]
    [TestCase("a", "a", true)]
    [TestCase("a", "b", false)]
    [TestCase("/a", "/a", true)]
    [TestCase("/a", "a/", false)]
    [TestCase("/a", "a\\", false)]
    public void ShouldCalculateIsSamePath(string first, string second, bool expected)
    {
        //Given
        //When
        var result = PathUtils.IsSamePath(first, second);

        //Then
        result.ShouldBe(expected);
    }

    [Test]
    public void ShouldCalculatePlatformSpecificIsSamePath()
    {
        //Given
        var first = Path.Combine("a", "b");

        //When
        var second = PathUtils.IsWindows ? "a\\b" : "/a/b";

        //Then
        PathUtils.IsSamePath(first, second).ShouldBe(true);
    }
}