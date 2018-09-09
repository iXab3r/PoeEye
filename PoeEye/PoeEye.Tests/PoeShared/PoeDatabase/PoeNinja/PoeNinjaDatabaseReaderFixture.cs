using System.Reactive.Concurrency;
using NUnit.Framework;
using PoeShared.PoeDatabase.PoeNinja;
using Shouldly;

namespace PoeEye.Tests.PoeShared.PoeDatabase.PoeNinja
{
    [TestFixture]
    public class PoeNinjaDatabaseReaderFixture
    {
        [SetUp]
        public void SetUp()
        {
        }

        private PoeNinjaDatabaseReader CreateInstance()
        {
            return new PoeNinjaDatabaseReader(Scheduler.Immediate, Scheduler.Immediate);
        }

        [Test]
        public void ShouldCreate()
        {
            //Then
            CreateInstance();
        }

        [Test]
        public void ShouldRead()
        {
            //Given
            var instance = CreateInstance();

            //When
            var result = instance.KnownEntityNames;

            //Then
            result.Count.ShouldNotBe(0);
        }
    }
}