using System;
using System.IO;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.FileProviders;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class InMemoryFileProviderFixture : FileProviderContractFixtureBase
{
    protected override IFileProvider ProviderUnderTest => InMemoryFileProvider;

    [Test]
    public void ShouldSupportExplicitEmptyDirectoryEntries()
    {
        //Given
        var instance = new InMemoryFileProvider();
        instance.FilesByName.AddOrUpdate(new DirectoryFileInfoStub("generated/empty", new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero)));

        //When
        var rootContents = instance.GetDirectoryContents(string.Empty).ToArray();
        var generatedContents = instance.GetDirectoryContents("generated").ToArray();
        var emptyContents = instance.GetDirectoryContents("generated/empty").ToArray();

        //Then
        rootContents.Select(x => x.Name).ShouldBe(new[] { "generated" });
        generatedContents.Select(x => x.Name).ShouldBe(new[] { "empty" });
        instance.GetFileInfo("generated").Exists.ShouldBe(false);
        instance.GetFileInfo("generated/empty").Exists.ShouldBe(false);
        emptyContents.ShouldBeEmpty();
        instance.GetDirectoryContents("generated/empty").Exists.ShouldBe(true);
    }

    [Test]
    public void ShouldRefreshSnapshotAfterFilesByNameMutation()
    {
        //Given
        InMemoryFileProvider.GetDirectoryContents(string.Empty).Exists.ShouldBe(true);

        //When
        InMemoryFileProvider.FilesByName.AddOrUpdate(new InMemoryFileInfo("later/created.txt", System.Text.Encoding.UTF8.GetBytes("created"), new DateTimeOffset(2024, 7, 28, 0, 0, 0, TimeSpan.Zero)));

        //Then
        InMemoryFileProvider.GetFileInfo("later/created.txt").Exists.ShouldBe(true);
        InMemoryFileProvider.GetDirectoryContents("later").Select(x => x.Name).ShouldBe(new[] { "created.txt" });
    }

    private sealed class DirectoryFileInfoStub : IFileInfo
    {
        public DirectoryFileInfoStub(string name, DateTimeOffset lastModified)
        {
            Name = name;
            LastModified = lastModified;
        }

        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Directory entries are not streamable.");
        }

        public bool Exists => true;

        public bool IsDirectory => true;

        public DateTimeOffset LastModified { get; }

        public long Length => -1;

        public string Name { get; }

        public string PhysicalPath => null;
    }
}
