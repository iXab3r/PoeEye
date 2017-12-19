using System.Reactive.Concurrency;
using PoeShared.PoeDatabase;

namespace PoeEye.Tests.PoeShared.PoeDatabase
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class StaticPoeDatabaseReaderFixture
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

        private StaticPoeDatabaseReader CreateInstance()
        {
            return new StaticPoeDatabaseReader(Scheduler.Immediate);
        }
    }
}