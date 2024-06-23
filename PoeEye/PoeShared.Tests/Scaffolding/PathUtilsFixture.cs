﻿using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [TestCase("f\\a", "f\\a - Copy (2)", "f\\a", "f\\b")]
    [TestCase("f\\a.json", "f\\a - Copy (2).json", "f\\a.json", "f\\b.json")]
    [TestCase("a", "a - Copy (2)", "a")]
    [TestCase("1. a", "1. a - Copy (2)", "1. a")]
    [TestCase("1. a.json", "1. a - Copy (2).json", "1. a.json")]
    [TestCase("1. a.json.enc", "1. a - Copy (2).json.enc", "1. a.json.enc")]
    [TestCase("a.json", "a - Copy (2).json", "a.json")]
    [TestCase("a.enc.json", "a - Copy (2).enc.json", "a.enc.json")]
    [TestCase("a", "a - Copy (2)", "b", "a")]
    [TestCase("a - Copy (2)", "a - Copy (3)", "a - Copy (2)")]
    [TestCase("a - Copy (3)", "a - Copy (4)", "a - Copy (3)")]
    [TestCase("a - Copy (3) ", "a - Copy (3)  - Copy (2)", "a - Copy (3) ")]
    [TestCase("Test2\\TG Window is active - Copy (2)", "Test2\\TG Window is active - Copy (3)", "Test2\\TG Window is active - Copy (2)")]
    [TestCase("Test2\\TG Window is active - Copy (2)", "Test2\\TG Window is active - Copy (4)", "Test2\\TG Window is active - Copy (2)","Test2\\TG Window is active - Copy (3)")]
    [TestCase("Test2 - Copy (2)\\TG Window is active - Copy (2)", "Test2 - Copy (2)\\TG Window is active - Copy (3)", "Test2 - Copy (2)\\TG Window is active - Copy (2)")]
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

    [Test]
    [TestCase("example.txt", "example")]
    [TestCase("anotherExample.json", "anotherExample")]
    [TestCase("noExtension", "noExtension")]
    [TestCase(".hiddenFile", "")]
    [TestCase("complex.file.name.txt", "complex.file.name")]
    [TestCase("a", "a")]
    [TestCase("", "")]
    [TestCase(null, null)]
    public void ShouldReturnFileNameWithoutExtension(string path, string expected)
    {
        // Given
        // When
        var fileName = Path.GetFileNameWithoutExtension(path);

        // Then
        fileName.ShouldBe(expected);
    }
    
    [Test]
    [TestCase("example.txt", "example", new string[] { ".txt" })]
    [TestCase("example.archive.log", "example.archive", new string[] { ".log" })]
    [TestCase("example.tar.gz", "example", new string[] { ".gz", ".tar" })]
    [TestCase("example.tar.gz", "example.tar", new string[] { ".gz" })]
    [TestCase("example.tar.gz", "example.tar.gz", new string[] { ".zip" })]
    [TestCase("example.", "example", new string[] { "." })]
    [TestCase("example..", "example", new string[] { "." })]
    [TestCase("example.test.json", "example.test", new string[] { ".json" })]
    [TestCase("example.test.json", "example", new string[] { ".json", ".test" })]
    [TestCase("example.tar.gz.backup", "example.tar.gz", new string[] { ".backup" })]
    [TestCase("example.tar.gz.backup", "example.tar", new string[] { ".backup", ".gz" })]
    [TestCase("example.tar.gz.backup", "example", new string[] { ".backup", ".gz", ".tar" })]
    [TestCase("example", "example", new string[] { ".txt", ".log" })]
    [TestCase("archive.tar.gz", "archive", new string[] { ".tar", ".gz" })]
    [TestCase("archive.tar.gz.zip", "archive.tar.gz", new string[] { ".zip" })]
    [TestCase("archive.tar.gz.zip", "archive.tar", new string[] { ".zip", ".gz" })]
    [TestCase("file.with.many.dots.ext", "file.with.many.dots", new string[] { ".ext" })]
    [TestCase("file.with.many.dots.ext", "file.with.many", new string[] { ".ext", ".dots" })]
    [TestCase("file..double.dots..ext", "file..double.dots.", new string[] { ".ext" })]
    [TestCase("", "", new string[] { ".txt" })]
    [TestCase(null, null, new string[] { ".txt" })]
    public void ShouldRemoveExtensions(string path, string expected, params string[] extensions)
    {
        // Given
        // When
        var fileName = PathUtils.RemoveExtensions(path, extensions.ToHashSet());

        // Then
        fileName.ShouldBe(expected);
    }
}