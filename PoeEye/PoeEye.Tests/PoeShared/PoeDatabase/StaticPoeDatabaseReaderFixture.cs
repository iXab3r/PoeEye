using System.Reactive.Concurrency;
using NUnit.Framework;
using PoeShared.PoeDatabase;
using Shouldly;

namespace PoeEye.Tests.PoeShared.PoeDatabase
{
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