using NUnit.Framework;
using AutoFixture;
using System;
using System.IO;
using System.Linq;
using PoeShared.Services;
using Shouldly;

namespace PoeShared.Tests.Services
{
    [TestFixture]
    public class SevenZipWrappeTests : FixtureBase
    {
        private static readonly string TempFolderName = nameof(SevenZipWrappeTests);

        private DirectoryInfo tempFolder;
        
        protected override void SetUp()
        {
            tempFolder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), TempFolderName));
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
            else
            {
                tempFolder.Create();
            }
        }

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1024)]
        [TestCase(1024*1024)]
        [TestCase(1024*1024*1024)]
        public void ShouldCompress(int length)
        {
            //Given
            var instance = CreateInstance();
            var inputFile = GenerateFile(Path.Combine(tempFolder.FullName, nameof(ShouldCompress)), length);

            var outputFile = new FileInfo( Path.Combine(tempFolder.FullName, $"{nameof(ShouldCompress)}.7z"));
            
            //When
            instance.AddToArchive(outputFile, new []{ inputFile });

            //Then
            outputFile.Refresh();
            outputFile.Exists.ShouldBe(true);
            outputFile.Length.ShouldBeGreaterThan(0);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1024)]
        [TestCase(1024*1024)]
        [TestCase(1024*1024*1024)]
        public void ShouldExtract(int length)
        {
            //Given
            var instance = CreateInstance();
            var inputFile = GenerateFile(Path.Combine(tempFolder.FullName, nameof(ShouldExtract)), length);
            var outputFile = new FileInfo( Path.Combine(tempFolder.FullName, $"{nameof(ShouldExtract)}.7z"));
            var outputDirectory = new DirectoryInfo(Path.Combine(tempFolder.FullName, $"{nameof(ShouldExtract)}_out"));
            instance.AddToArchive(outputFile, new []{ inputFile });

            //When
            instance.ExtractArchive(outputFile, outputDirectory);

            //Then
            outputDirectory.Refresh();
            outputDirectory.Exists.ShouldBe(true);
            outputDirectory.GetFiles().Select(x => x.Name).ShouldBe(new[]{ inputFile.Name });
        }

        private FileInfo GenerateFile(string filePath, int length)
        {
            var directory = Path.GetDirectoryName(filePath);
            Directory.CreateDirectory(directory);
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(length);
            }

            return new FileInfo(filePath);
        }

        private SevenZipWrapper CreateInstance()
        {
            return Container.Build<SevenZipWrapper>().Create();
        }
    }
}