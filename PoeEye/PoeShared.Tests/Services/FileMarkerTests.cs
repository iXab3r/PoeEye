using NUnit.Framework;
using System;
using System.IO;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Services;

[TestFixture]
public class FileMarkerTests : FixtureBase
{
    private FileInfo lockFile;

    protected override void SetUp()
    {
        lockFile = new FileInfo(Path.Combine(Path.GetTempPath(), $".{nameof(FileMarkerTests)}"));
        if (lockFile.Exists)
        {
            lockFile.Delete();
        }
    }

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        Action action = () =>
        {
            using (var instance = CreateInstance())
            {
            }
        };

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldCreateLockFile()
    {
        //Given
        using var instance = CreateInstance();

        //When

        //Then
        instance.Exists.ShouldBe(true);
        instance.ExistedInitially.ShouldBe(false);
    }

    [Test]
    public void ShouldRemoveLockFileOnDisposal()
    {
        //Given
        var instance = CreateInstance();
        instance.Exists.ShouldBe(true);

        //When
        instance.Dispose();

        //Then
        instance.Exists.ShouldBe(false);
    }

    [Test]
    public void ShouldLockFile()
    {
        //Given
        using var instance = CreateInstance();

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldThrow<IOException>();
    }

    [Test]
    public void ShouldDetectIfFileExistedInitially()
    {
        //Given
        File.WriteAllText(lockFile.FullName, "test");

        //When
        using var instance = CreateInstance();

        //Then
        instance.Exists.ShouldBe(true);
        instance.ExistedInitially.ShouldBe(true);
    }

    private FileMarker CreateInstance()
    {
        return new FileMarker(lockFile);
    }
}