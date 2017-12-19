using System.Reactive.Concurrency;
using PoeShared.PoeDatabase.PoeNinja;

namespace PoeEye.Tests.PoeShared.PoeDatabase.PoeNinja
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class PoeNinjaDatabaseReaderFixture
    {
        [SetUp]
        public void SetUp() { }

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

        private PoeNinjaDatabaseReader CreateInstance()
        {
            return new PoeNinjaDatabaseReader(Scheduler.Immediate, Scheduler.Immediate);
        }
    }
}